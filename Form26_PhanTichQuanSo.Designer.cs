namespace PhanMemThiDua2026
{
    partial class Form26_PhanTichQuanSo
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form26_PhanTichQuanSo));
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            toolStripStatusLabel2 = new ToolStripStatusLabel();
            tableLayoutPanel1 = new TableLayoutPanel();
            kryptonButton1_Dong = new Krypton.Toolkit.KryptonButton();
            label_DonVi = new Label();
            pictureBox1 = new PictureBox();
            tableLayoutPanel2 = new TableLayoutPanel();
            kryptonDataGridView1 = new Krypton.Toolkit.KryptonDataGridView();
            toolTip1 = new ToolTip(components);
            statusStrip1.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)kryptonDataGridView1).BeginInit();
            SuspendLayout();
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(21, 21);
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1, toolStripStatusLabel2 });
            statusStrip1.Location = new Point(0, 655);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 10, 0);
            statusStrip1.Size = new Size(1264, 26);
            statusStrip1.TabIndex = 0;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Image = (Image)resources.GetObject("toolStripStatusLabel1.Image");
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(139, 21);
            toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // toolStripStatusLabel2
            // 
            toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            toolStripStatusLabel2.Size = new Size(118, 21);
            toolStripStatusLabel2.Text = "toolStripStatusLabel2";
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5.961844F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 78.77583F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15.1865005F));
            tableLayoutPanel1.Controls.Add(kryptonButton1_Dong, 2, 0);
            tableLayoutPanel1.Controls.Add(label_DonVi, 1, 0);
            tableLayoutPanel1.Controls.Add(pictureBox1, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(3, 3);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(1258, 42);
            tableLayoutPanel1.TabIndex = 1;
            // 
            // kryptonButton1_Dong
            // 
            kryptonButton1_Dong.Anchor = AnchorStyles.None;
            kryptonButton1_Dong.Location = new Point(1106, 6);
            kryptonButton1_Dong.Name = "kryptonButton1_Dong";
            kryptonButton1_Dong.Size = new Size(112, 30);
            kryptonButton1_Dong.StateCommon.Border.Rounding = 4F;
            kryptonButton1_Dong.TabIndex = 3;
            kryptonButton1_Dong.Values.DropDownArrowColor = Color.Empty;
            kryptonButton1_Dong.Values.Image = (Image)resources.GetObject("kryptonButton1_Dong.Values.Image");
            kryptonButton1_Dong.Values.Text = "Đóng";
            kryptonButton1_Dong.Click += kryptonButton1_Dong_Click;
            // 
            // label_DonVi
            // 
            label_DonVi.Anchor = AnchorStyles.Left;
            label_DonVi.AutoSize = true;
            label_DonVi.Font = new Font("Segoe UI Semibold", 9.792F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            label_DonVi.ForeColor = Color.Green;
            label_DonVi.Location = new Point(78, 11);
            label_DonVi.Name = "label_DonVi";
            label_DonVi.Size = new Size(50, 19);
            label_DonVi.TabIndex = 25;
            label_DonVi.Text = "Đơn vị";
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.None;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(3, 4);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(69, 33);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 1;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Controls.Add(tableLayoutPanel1, 0, 0);
            tableLayoutPanel2.Controls.Add(kryptonDataGridView1, 0, 1);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(0, 0);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 3;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 7.4438014F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 51.3808975F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 41.1753044F));
            tableLayoutPanel2.Size = new Size(1264, 655);
            tableLayoutPanel2.TabIndex = 2;
            tableLayoutPanel2.Paint += tableLayoutPanel2_Paint;
            // 
            // kryptonDataGridView1
            // 
            kryptonDataGridView1.BorderStyle = BorderStyle.None;
            kryptonDataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            kryptonDataGridView1.Dock = DockStyle.Fill;
            kryptonDataGridView1.Location = new Point(3, 51);
            kryptonDataGridView1.Name = "kryptonDataGridView1";
            kryptonDataGridView1.RowHeadersWidth = 53;
            kryptonDataGridView1.Size = new Size(1258, 330);
            kryptonDataGridView1.TabIndex = 2;
            // 
            // Form26_PhanTichQuanSo
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1264, 681);
            Controls.Add(tableLayoutPanel2);
            Controls.Add(statusStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form26_PhanTichQuanSo";
            Text = "Phân tích thành phần quân số";
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            tableLayoutPanel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)kryptonDataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private StatusStrip statusStrip1;
        private TableLayoutPanel tableLayoutPanel1;
        private PictureBox pictureBox1;
        private TableLayoutPanel tableLayoutPanel2;
        private Krypton.Toolkit.KryptonDataGridView kryptonDataGridView1;
        internal Krypton.Toolkit.KryptonButton kryptonButton1_Dong;
        private Label label_DonVi;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripStatusLabel toolStripStatusLabel2;
        private ToolTip toolTip1;
    }
}