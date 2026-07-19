using System.Diagnostics;

namespace PhanMemThiDua2026
{
    partial class Form24_XacMinhAdmin
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>


        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form24_XacMinhAdmin));
            PictureBox1 = new PictureBox();
            check_HienMatKhau = new CheckBox();
            text_MatKhau = new Krypton.Toolkit.KryptonTextBox();
            label2 = new Label();
            btn_XacThuc = new Krypton.Toolkit.KryptonButton();
            TableLayoutPanel3 = new TableLayoutPanel();
            btn_Thoat = new Krypton.Toolkit.KryptonButton();
            TableLayoutPanel1 = new TableLayoutPanel();
            TableLayoutPanel2 = new TableLayoutPanel();
            text_TenDangNhap = new Krypton.Toolkit.KryptonTextBox();
            label1 = new Label();
            TableLayoutPanel5 = new TableLayoutPanel();
            toolTip1 = new ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)PictureBox1).BeginInit();
            TableLayoutPanel3.SuspendLayout();
            TableLayoutPanel1.SuspendLayout();
            TableLayoutPanel2.SuspendLayout();
            TableLayoutPanel5.SuspendLayout();
            SuspendLayout();
            // 
            // PictureBox1
            // 
            PictureBox1.Anchor = AnchorStyles.None;
            PictureBox1.Image = (Image)resources.GetObject("PictureBox1.Image");
            PictureBox1.Location = new Point(9, 33);
            PictureBox1.Name = "PictureBox1";
            PictureBox1.Size = new Size(137, 124);
            PictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            PictureBox1.TabIndex = 0;
            PictureBox1.TabStop = false;
            // 
            // check_HienMatKhau
            // 
            check_HienMatKhau.Anchor = AnchorStyles.None;
            check_HienMatKhau.AutoSize = true;
            check_HienMatKhau.Font = new Font("Segoe UI", 10.2F, FontStyle.Italic, GraphicsUnit.Point, 0);
            check_HienMatKhau.ForeColor = Color.Red;
            check_HienMatKhau.Location = new Point(170, 111);
            check_HienMatKhau.Margin = new Padding(2);
            check_HienMatKhau.Name = "check_HienMatKhau";
            check_HienMatKhau.Size = new Size(121, 23);
            check_HienMatKhau.TabIndex = 0;
            check_HienMatKhau.Text = "Hiện mật khẩu";
            check_HienMatKhau.UseVisualStyleBackColor = true;
            // 
            // text_MatKhau
            // 
            text_MatKhau.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            text_MatKhau.Location = new Point(154, 61);
            text_MatKhau.Margin = new Padding(2);
            text_MatKhau.Name = "text_MatKhau";
            text_MatKhau.Size = new Size(282, 29);
            text_MatKhau.StateCommon.Border.Rounding = 8F;
            text_MatKhau.StateCommon.Border.Width = 1;
            text_MatKhau.TabIndex = 23;
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Right;
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 10.8F, FontStyle.Italic);
            label2.ForeColor = Color.Blue;
            label2.Location = new Point(81, 65);
            label2.Margin = new Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new Size(69, 20);
            label2.TabIndex = 24;
            label2.Text = "Mật khẩu";
            // 
            // btn_XacThuc
            // 
            btn_XacThuc.Anchor = AnchorStyles.Top;
            btn_XacThuc.Location = new Point(281, 2);
            btn_XacThuc.Margin = new Padding(2);
            btn_XacThuc.Name = "btn_XacThuc";
            btn_XacThuc.Size = new Size(124, 30);
            btn_XacThuc.StateCommon.Border.Rounding = 4F;
            btn_XacThuc.TabIndex = 0;
            btn_XacThuc.Values.DropDownArrowColor = Color.Empty;
            btn_XacThuc.Values.Image = (Image)resources.GetObject("btn_XacThuc.Values.Image");
            btn_XacThuc.Values.Text = "Xác thực";
            btn_XacThuc.Click += btn_XacThuc_Click;
            // 
            // TableLayoutPanel3
            // 
            TableLayoutPanel3.ColumnCount = 2;
            TableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            TableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            TableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 18F));
            TableLayoutPanel3.Controls.Add(btn_XacThuc, 1, 0);
            TableLayoutPanel3.Controls.Add(btn_Thoat, 0, 0);
            TableLayoutPanel3.Dock = DockStyle.Fill;
            TableLayoutPanel3.Location = new Point(2, 143);
            TableLayoutPanel3.Margin = new Padding(2);
            TableLayoutPanel3.Name = "TableLayoutPanel3";
            TableLayoutPanel3.RowCount = 1;
            TableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            TableLayoutPanel3.Size = new Size(458, 41);
            TableLayoutPanel3.TabIndex = 10;
            // 
            // btn_Thoat
            // 
            btn_Thoat.Anchor = AnchorStyles.Top;
            btn_Thoat.Location = new Point(52, 2);
            btn_Thoat.Margin = new Padding(2);
            btn_Thoat.Name = "btn_Thoat";
            btn_Thoat.Size = new Size(124, 30);
            btn_Thoat.StateCommon.Border.Rounding = 4F;
            btn_Thoat.TabIndex = 1;
            btn_Thoat.Values.DropDownArrowColor = Color.Empty;
            btn_Thoat.Values.Image = (Image)resources.GetObject("btn_Thoat.Values.Image");
            btn_Thoat.Values.Text = "Thoát";
            btn_Thoat.Click += btn_Thoat_Click;
            // 
            // TableLayoutPanel1
            // 
            TableLayoutPanel1.ColumnCount = 1;
            TableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            TableLayoutPanel1.Controls.Add(TableLayoutPanel3, 0, 2);
            TableLayoutPanel1.Controls.Add(TableLayoutPanel2, 0, 0);
            TableLayoutPanel1.Controls.Add(check_HienMatKhau, 0, 1);
            TableLayoutPanel1.Dock = DockStyle.Fill;
            TableLayoutPanel1.Location = new Point(158, 2);
            TableLayoutPanel1.Margin = new Padding(2);
            TableLayoutPanel1.Name = "TableLayoutPanel1";
            TableLayoutPanel1.RowCount = 3;
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 56.9037666F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 19.6652718F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 23.8493729F));
            TableLayoutPanel1.Size = new Size(462, 186);
            TableLayoutPanel1.TabIndex = 9;
            // 
            // TableLayoutPanel2
            // 
            TableLayoutPanel2.ColumnCount = 3;
            TableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.09859F));
            TableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62.4413147F));
            TableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 4.159132F));
            TableLayoutPanel2.Controls.Add(text_MatKhau, 1, 1);
            TableLayoutPanel2.Controls.Add(text_TenDangNhap, 1, 0);
            TableLayoutPanel2.Controls.Add(label2, 0, 1);
            TableLayoutPanel2.Controls.Add(label1, 0, 0);
            TableLayoutPanel2.Dock = DockStyle.Fill;
            TableLayoutPanel2.Location = new Point(2, 2);
            TableLayoutPanel2.Margin = new Padding(2);
            TableLayoutPanel2.Name = "TableLayoutPanel2";
            TableLayoutPanel2.RowCount = 2;
            TableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            TableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            TableLayoutPanel2.Size = new Size(458, 101);
            TableLayoutPanel2.TabIndex = 1;
            // 
            // text_TenDangNhap
            // 
            text_TenDangNhap.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            text_TenDangNhap.Location = new Point(154, 10);
            text_TenDangNhap.Margin = new Padding(2);
            text_TenDangNhap.Name = "text_TenDangNhap";
            text_TenDangNhap.Size = new Size(282, 29);
            text_TenDangNhap.StateCommon.Border.Rounding = 8F;
            text_TenDangNhap.StateCommon.Border.Width = 1;
            text_TenDangNhap.TabIndex = 23;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 10.8F, FontStyle.Italic);
            label1.ForeColor = Color.Blue;
            label1.Location = new Point(46, 15);
            label1.Margin = new Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new Size(104, 20);
            label1.TabIndex = 24;
            label1.Text = "Tên đăng nhập";
            // 
            // TableLayoutPanel5
            // 
            TableLayoutPanel5.ColumnCount = 2;
            TableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25.1993618F));
            TableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 74.80064F));
            TableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 18F));
            TableLayoutPanel5.Controls.Add(PictureBox1, 0, 0);
            TableLayoutPanel5.Controls.Add(TableLayoutPanel1, 1, 0);
            TableLayoutPanel5.Dock = DockStyle.Fill;
            TableLayoutPanel5.Location = new Point(0, 0);
            TableLayoutPanel5.Margin = new Padding(2);
            TableLayoutPanel5.Name = "TableLayoutPanel5";
            TableLayoutPanel5.RowCount = 1;
            TableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            TableLayoutPanel5.Size = new Size(622, 190);
            TableLayoutPanel5.TabIndex = 13;
            // 
            // Form24_XacMinhAdmin
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(622, 190);
            Controls.Add(TableLayoutPanel5);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(2);
            Name = "Form24_XacMinhAdmin";
            Text = "Xác thực quyền admin";
            Load += Form24_XacMinhAdmin_Load;
            ((System.ComponentModel.ISupportInitialize)PictureBox1).EndInit();
            TableLayoutPanel3.ResumeLayout(false);
            TableLayoutPanel1.ResumeLayout(false);
            TableLayoutPanel1.PerformLayout();
            TableLayoutPanel2.ResumeLayout(false);
            TableLayoutPanel2.PerformLayout();
            TableLayoutPanel5.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        internal PictureBox PictureBox1;
        internal CheckBox check_HienMatKhau;
        private TableLayoutPanel tableLayoutPanel7;
        private Krypton.Toolkit.KryptonButton btn_Thoat;
        private Krypton.Toolkit.KryptonButton btn_XacThuc;
        internal TableLayoutPanel TableLayoutPanel3;
        internal TableLayoutPanel TableLayoutPanel1;
        internal TableLayoutPanel TableLayoutPanel2;
        internal TableLayoutPanel TableLayoutPanel5;
        private ToolTip toolTip1;
        private Krypton.Toolkit.KryptonTextBox text_MatKhau;
        private Krypton.Toolkit.KryptonTextBox text_TenDangNhap;
        private Label label2;
        private Label label1;
    }
}