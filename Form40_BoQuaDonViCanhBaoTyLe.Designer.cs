namespace PhanMemThiDua2026
{
    partial class Form40_BoQuaDonViCanhBaoTyLe
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form40_BoQuaDonViCanhBaoTyLe));
            tableLayoutPanel1 = new TableLayoutPanel();
            label9 = new Label();
            pictureBox1 = new PictureBox();
            tableLayoutPanel2 = new TableLayoutPanel();
            kryptonButton1_DongFrom = new Krypton.Toolkit.KryptonButton();
            checkedListBox1_ChonDonViBoQuaTyLe = new CheckedListBox();
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            tableLayoutPanel2.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.6757679F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 85.324234F));
            tableLayoutPanel1.Controls.Add(label9, 1, 0);
            tableLayoutPanel1.Controls.Add(pictureBox1, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(3, 3);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Size = new Size(586, 53);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // label9
            // 
            label9.Anchor = AnchorStyles.Left;
            label9.AutoSize = true;
            label9.Font = new Font("Segoe UI", 10.2F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label9.ForeColor = Color.Green;
            label9.Location = new Point(88, 17);
            label9.Margin = new Padding(2, 0, 2, 0);
            label9.Name = "label9";
            label9.Size = new Size(411, 19);
            label9.TabIndex = 26;
            label9.Text = "Bỏ qua cảnh báo màu đỏ khi không đạt tỷ lệ ở Bang1 Trang chủ";
            // 
            // pictureBox1
            // 
            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(3, 3);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(80, 47);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            pictureBox1.Click += pictureBox1_Click;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 1;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Controls.Add(kryptonButton1_DongFrom, 0, 2);
            tableLayoutPanel2.Controls.Add(tableLayoutPanel1, 0, 0);
            tableLayoutPanel2.Controls.Add(checkedListBox1_ChonDonViBoQuaTyLe, 0, 1);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(0, 0);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 3;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 16.304348F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 71.1956558F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 12.771739F));
            tableLayoutPanel2.Size = new Size(592, 368);
            tableLayoutPanel2.TabIndex = 1;
            // 
            // kryptonButton1_DongFrom
            // 
            kryptonButton1_DongFrom.Anchor = AnchorStyles.None;
            kryptonButton1_DongFrom.Location = new Point(240, 329);
            kryptonButton1_DongFrom.Name = "kryptonButton1_DongFrom";
            kryptonButton1_DongFrom.Size = new Size(112, 30);
            kryptonButton1_DongFrom.StateCommon.Border.Rounding = 4F;
            kryptonButton1_DongFrom.TabIndex = 4;
            kryptonButton1_DongFrom.Values.DropDownArrowColor = Color.Empty;
            kryptonButton1_DongFrom.Values.Image = (Image)resources.GetObject("kryptonButton1_DongFrom.Values.Image");
            kryptonButton1_DongFrom.Values.Text = "Đóng";
            kryptonButton1_DongFrom.Click += kryptonButton1_DongFrom_Click;
            // 
            // checkedListBox1_ChonDonViBoQuaTyLe
            // 
            checkedListBox1_ChonDonViBoQuaTyLe.Dock = DockStyle.Fill;
            checkedListBox1_ChonDonViBoQuaTyLe.Font = new Font("Segoe UI", 12F, FontStyle.Italic, GraphicsUnit.Point, 0);
            checkedListBox1_ChonDonViBoQuaTyLe.ForeColor = Color.FromArgb(0, 0, 192);
            checkedListBox1_ChonDonViBoQuaTyLe.FormattingEnabled = true;
            checkedListBox1_ChonDonViBoQuaTyLe.Location = new Point(3, 62);
            checkedListBox1_ChonDonViBoQuaTyLe.Name = "checkedListBox1_ChonDonViBoQuaTyLe";
            checkedListBox1_ChonDonViBoQuaTyLe.Size = new Size(586, 255);
            checkedListBox1_ChonDonViBoQuaTyLe.TabIndex = 1;
            // 
            // Form40_BoQuaDonViCanhBaoTyLe
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(592, 368);
            Controls.Add(tableLayoutPanel2);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form40_BoQuaDonViCanhBaoTyLe";
            Text = "Chọn các đơn vị không cần cảnh báo tỷ lệ";
            Load += Form40_BoQuaDonViCanhBaoTyLe_Load;
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            tableLayoutPanel2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private PictureBox pictureBox1;
        private TableLayoutPanel tableLayoutPanel2;
        private CheckedListBox checkedListBox1_ChonDonViBoQuaTyLe;
        private Label label9;
        internal Krypton.Toolkit.KryptonButton kryptonButton1_DongFrom;
    }
}