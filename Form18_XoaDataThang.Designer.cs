namespace PhanMemThiDua2026
{
    partial class Form18_XoaDataThang
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form18_XoaDataThang));
            tableLayoutPanel1 = new TableLayoutPanel();
            tableLayoutPanel2 = new TableLayoutPanel();
            kryptonButton_XoaDuLieuThangThongKe = new Krypton.Toolkit.KryptonButton();
            tableLayoutPanel3 = new TableLayoutPanel();
            label1 = new Label();
            comboBox1_ChonThangCanXoaDuLieu = new ComboBox();
            pictureBox1 = new PictureBox();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18.4534264F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 81.54657F));
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 1, 0);
            tableLayoutPanel1.Controls.Add(pictureBox1, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(3, 2, 3, 2);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 14F));
            tableLayoutPanel1.Size = new Size(544, 110);
            tableLayoutPanel1.TabIndex = 1;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 1;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Controls.Add(kryptonButton_XoaDuLieuThangThongKe, 0, 1);
            tableLayoutPanel2.Controls.Add(tableLayoutPanel3, 0, 0);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(103, 2);
            tableLayoutPanel2.Margin = new Padding(3, 2, 3, 2);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 2;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.Size = new Size(438, 106);
            tableLayoutPanel2.TabIndex = 2;
            // 
            // kryptonButton_XoaDuLieuThangThongKe
            // 
            kryptonButton_XoaDuLieuThangThongKe.Anchor = AnchorStyles.None;
            kryptonButton_XoaDuLieuThangThongKe.Location = new Point(154, 66);
            kryptonButton_XoaDuLieuThangThongKe.Margin = new Padding(3, 2, 3, 2);
            kryptonButton_XoaDuLieuThangThongKe.Name = "kryptonButton_XoaDuLieuThangThongKe";
            kryptonButton_XoaDuLieuThangThongKe.Size = new Size(130, 27);
            kryptonButton_XoaDuLieuThangThongKe.StateCommon.Border.Rounding = 4F;
            kryptonButton_XoaDuLieuThangThongKe.TabIndex = 1;
            kryptonButton_XoaDuLieuThangThongKe.Values.DropDownArrowColor = Color.Empty;
            kryptonButton_XoaDuLieuThangThongKe.Values.Image = (Image)resources.GetObject("kryptonButton_XoaDuLieuThangThongKe.Values.Image");
            kryptonButton_XoaDuLieuThangThongKe.Values.Text = " Xóa dữ liệu";
            kryptonButton_XoaDuLieuThangThongKe.Click += kryptonButton_XoaDuLieuThangThongKe_Click;
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.ColumnCount = 2;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29.841898F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70.1581039F));
            tableLayoutPanel3.Controls.Add(label1, 0, 0);
            tableLayoutPanel3.Controls.Add(comboBox1_ChonThangCanXoaDuLieu, 1, 0);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new Point(3, 2);
            tableLayoutPanel3.Margin = new Padding(3, 2, 3, 2);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 1;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.Size = new Size(432, 49);
            tableLayoutPanel3.TabIndex = 4;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Left;
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 9.216F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.Red;
            label1.Location = new Point(3, 16);
            label1.Name = "label1";
            label1.Size = new Size(78, 17);
            label1.TabIndex = 1;
            label1.Text = "Chọn tháng";
            // 
            // comboBox1_ChonThangCanXoaDuLieu
            // 
            comboBox1_ChonThangCanXoaDuLieu.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            comboBox1_ChonThangCanXoaDuLieu.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1_ChonThangCanXoaDuLieu.Font = new Font("Segoe UI", 9.792F);
            comboBox1_ChonThangCanXoaDuLieu.FormattingEnabled = true;
            comboBox1_ChonThangCanXoaDuLieu.Location = new Point(131, 11);
            comboBox1_ChonThangCanXoaDuLieu.Margin = new Padding(3, 2, 3, 2);
            comboBox1_ChonThangCanXoaDuLieu.Name = "comboBox1_ChonThangCanXoaDuLieu";
            comboBox1_ChonThangCanXoaDuLieu.Size = new Size(298, 25);
            comboBox1_ChonThangCanXoaDuLieu.TabIndex = 3;
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.None;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(11, 21);
            pictureBox1.Margin = new Padding(3, 2, 3, 2);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(78, 68);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            // 
            // Form18_XoaDataThang
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(544, 110);
            Controls.Add(tableLayoutPanel1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 2, 3, 2);
            Name = "Form18_XoaDataThang";
            Text = "Xóa dữ liệu thi đua tháng";
            Load += Form18_Load;
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel3.ResumeLayout(false);
            tableLayoutPanel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel2;
        private PictureBox pictureBox1;
        internal Krypton.Toolkit.KryptonButton kryptonButton_XoaDuLieuThangThongKe;
        private TableLayoutPanel tableLayoutPanel3;
        private Label label1;
        private ComboBox comboBox1_ChonThangCanXoaDuLieu;
    }
}