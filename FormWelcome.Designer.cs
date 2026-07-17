namespace PhanMemThiDua2026
{
    partial class FormWelcome
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormWelcome));
            groupBox1 = new GroupBox();
            tableLayoutPanel1 = new TableLayoutPanel();
            tableLayoutPanel2 = new TableLayoutPanel();
            pictureBox1 = new PictureBox();
            tableLayoutPanel3 = new TableLayoutPanel();
            label2_TinCayAnToan = new Label();
            label1_TenPhanMem = new Label();
            tableLayoutPanel4 = new TableLayoutPanel();
            btn_BatDau = new Krypton.Toolkit.KryptonButton();
            richTextBox1 = new RichTextBox();
            timer1 = new System.Windows.Forms.Timer(components);
            groupBox1.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            tableLayoutPanel3.SuspendLayout();
            tableLayoutPanel4.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(tableLayoutPanel1);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Font = new Font("Segoe UI Semibold", 9.216F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            groupBox1.ForeColor = Color.FromArgb(0, 0, 192);
            groupBox1.Location = new Point(0, 0);
            groupBox1.Margin = new Padding(2);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(2);
            groupBox1.Size = new Size(567, 393);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Chào mừng đến với phần mềm thi đua phát triển năm 2026";
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 0, 0);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel4, 0, 1);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(2, 19);
            tableLayoutPanel1.Margin = new Padding(2);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 20.208334F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 79.7916641F));
            tableLayoutPanel1.Size = new Size(563, 372);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 2;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17.7981644F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 82.2018356F));
            tableLayoutPanel2.Controls.Add(pictureBox1, 0, 0);
            tableLayoutPanel2.Controls.Add(tableLayoutPanel3, 1, 0);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(2, 2);
            tableLayoutPanel2.Margin = new Padding(2);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.Size = new Size(559, 71);
            tableLayoutPanel2.TabIndex = 0;
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.None;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(14, 12);
            pictureBox1.Margin = new Padding(2);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(70, 47);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.ColumnCount = 1;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.Controls.Add(label2_TinCayAnToan, 0, 1);
            tableLayoutPanel3.Controls.Add(label1_TenPhanMem, 0, 0);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new Point(101, 2);
            tableLayoutPanel3.Margin = new Padding(2);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 2;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.Size = new Size(456, 67);
            tableLayoutPanel3.TabIndex = 1;
            // 
            // label2_TinCayAnToan
            // 
            label2_TinCayAnToan.Anchor = AnchorStyles.None;
            label2_TinCayAnToan.AutoSize = true;
            label2_TinCayAnToan.Font = new Font("Segoe UI", 9.216F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2_TinCayAnToan.ForeColor = Color.FromArgb(0, 0, 192);
            label2_TinCayAnToan.Location = new Point(71, 41);
            label2_TinCayAnToan.Margin = new Padding(2, 0, 2, 0);
            label2_TinCayAnToan.Name = "label2_TinCayAnToan";
            label2_TinCayAnToan.Size = new Size(314, 17);
            label2_TinCayAnToan.TabIndex = 2;
            label2_TinCayAnToan.Text = "Tin cậy – an toàn – bảo mật – phù hợp mọi máy tính";
            label2_TinCayAnToan.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label1_TenPhanMem
            // 
            label1_TenPhanMem.Anchor = AnchorStyles.None;
            label1_TenPhanMem.AutoSize = true;
            label1_TenPhanMem.Font = new Font("Segoe UI Semibold", 13.8239994F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1_TenPhanMem.ForeColor = Color.Blue;
            label1_TenPhanMem.Location = new Point(105, 4);
            label1_TenPhanMem.Margin = new Padding(2, 0, 2, 0);
            label1_TenPhanMem.Name = "label1_TenPhanMem";
            label1_TenPhanMem.Size = new Size(245, 25);
            label1_TenPhanMem.TabIndex = 1;
            label1_TenPhanMem.Text = "PHẦN MỀM THI ĐUA 2026";
            label1_TenPhanMem.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanel4
            // 
            tableLayoutPanel4.ColumnCount = 1;
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel4.Controls.Add(btn_BatDau, 0, 1);
            tableLayoutPanel4.Controls.Add(richTextBox1, 0, 0);
            tableLayoutPanel4.Dock = DockStyle.Fill;
            tableLayoutPanel4.Location = new Point(2, 77);
            tableLayoutPanel4.Margin = new Padding(2);
            tableLayoutPanel4.Name = "tableLayoutPanel4";
            tableLayoutPanel4.RowCount = 2;
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 81.56997F));
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 18.4300346F));
            tableLayoutPanel4.Size = new Size(559, 293);
            tableLayoutPanel4.TabIndex = 1;
            // 
            // btn_BatDau
            // 
            btn_BatDau.Anchor = AnchorStyles.None;
            btn_BatDau.Location = new Point(219, 251);
            btn_BatDau.Margin = new Padding(2);
            btn_BatDau.Name = "btn_BatDau";
            btn_BatDau.Size = new Size(120, 30);
            btn_BatDau.StateCommon.Border.Rounding = 4F;
            btn_BatDau.TabIndex = 19;
            btn_BatDau.Values.DropDownArrowColor = Color.Empty;
            btn_BatDau.Values.Image = (Image)resources.GetObject("btn_BatDau.Values.Image");
            btn_BatDau.Values.Text = "Bắt đầu";
            // 
            // richTextBox1
            // 
            richTextBox1.Dock = DockStyle.Fill;
            richTextBox1.Location = new Point(2, 2);
            richTextBox1.Margin = new Padding(2);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(555, 235);
            richTextBox1.TabIndex = 20;
            richTextBox1.Text = "";
            // 
            // FormWelcome
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(567, 393);
            Controls.Add(groupBox1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(2);
            Name = "FormWelcome";
            Text = "Lời chào đầu tiên";
            Load += FormWelcome_Load;
            groupBox1.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            tableLayoutPanel3.ResumeLayout(false);
            tableLayoutPanel3.PerformLayout();
            tableLayoutPanel4.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBox1;
        private TableLayoutPanel tableLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel2;
        private PictureBox pictureBox1;
        private TableLayoutPanel tableLayoutPanel3;
        private Label label2_TinCayAnToan;
        private Label label1_TenPhanMem;
        private TableLayoutPanel tableLayoutPanel4;
        internal Krypton.Toolkit.KryptonButton btn_BatDau;
        private RichTextBox richTextBox1;
        private System.Windows.Forms.Timer timer1;
    }
}