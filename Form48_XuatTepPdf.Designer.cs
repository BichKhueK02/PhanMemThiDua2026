namespace PhanMemThiDua2026
{
    partial class Form48_XuatTepPdf
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form48_XuatTepPdf));
            tableLayoutPanel1 = new TableLayoutPanel();
            tableLayoutPanel3 = new TableLayoutPanel();
            kryptonButton_XuatTepPdf = new Krypton.Toolkit.KryptonButton();
            checkBox1_ChonTatCa = new CheckBox();
            tableLayoutPanel2 = new TableLayoutPanel();
            pictureBox2 = new PictureBox();
            label_DuongDanPdf = new Label();
            kryptonButton1_ChonDuongDanLuuTepPdf = new Krypton.Toolkit.KryptonButton();
            label3 = new Label();
            tableLayoutPanel4 = new TableLayoutPanel();
            pictureBox1 = new PictureBox();
            label_DuongDanExcel = new Label();
            kryptonButton1_ChonDuongDanTepExcel = new Krypton.Toolkit.KryptonButton();
            label2 = new Label();
            statusStrip1 = new StatusStrip();
            toolStripProgressBar1_TienTrinhXuatTep = new ToolStripProgressBar();
            toolStripStatusLabel1_TongCongTepPdf = new ToolStripStatusLabel();
            toolStripStatusLabel1_DangLoad = new ToolStripStatusLabel();
            groupBox1 = new GroupBox();
            checkedListBox1_LietKeTenCacSheet = new CheckedListBox();
            toolTip1 = new ToolTip(components);
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            tableLayoutPanel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            statusStrip1.SuspendLayout();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(tableLayoutPanel3, 0, 3);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 0, 1);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel4, 0, 0);
            tableLayoutPanel1.Controls.Add(statusStrip1, 0, 4);
            tableLayoutPanel1.Controls.Add(groupBox1, 0, 2);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(3, 2, 3, 2);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 5;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 14.1165524F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 13.5937128F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 52.2835F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 13.1348276F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 6.8714F));
            tableLayoutPanel1.Size = new Size(917, 450);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.ColumnCount = 2;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 59.88764F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40.11236F));
            tableLayoutPanel3.Controls.Add(kryptonButton_XuatTepPdf, 1, 0);
            tableLayoutPanel3.Controls.Add(checkBox1_ChonTatCa, 0, 0);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new Point(3, 361);
            tableLayoutPanel3.Margin = new Padding(3, 2, 3, 2);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 1;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.Size = new Size(911, 55);
            tableLayoutPanel3.TabIndex = 14;
            // 
            // kryptonButton_XuatTepPdf
            // 
            kryptonButton_XuatTepPdf.Anchor = AnchorStyles.None;
            kryptonButton_XuatTepPdf.Location = new Point(612, 10);
            kryptonButton_XuatTepPdf.Margin = new Padding(3, 2, 3, 2);
            kryptonButton_XuatTepPdf.Name = "kryptonButton_XuatTepPdf";
            kryptonButton_XuatTepPdf.Size = new Size(232, 35);
            kryptonButton_XuatTepPdf.StateCommon.Border.Rounding = 16F;
            kryptonButton_XuatTepPdf.StateTracking.Back.Color1 = Color.FromArgb(128, 255, 128);
            kryptonButton_XuatTepPdf.StateTracking.Back.Color2 = Color.FromArgb(128, 255, 128);
            kryptonButton_XuatTepPdf.TabIndex = 0;
            kryptonButton_XuatTepPdf.Values.DropDownArrowColor = Color.Empty;
            kryptonButton_XuatTepPdf.Values.Image = (Image)resources.GetObject("kryptonButton_XuatTepPdf.Values.Image");
            kryptonButton_XuatTepPdf.Values.Text = "Tạo tệp (*.pdf)";
            kryptonButton_XuatTepPdf.Click += kryptonButton_XuatTepPdf_Click;
            // 
            // checkBox1_ChonTatCa
            // 
            checkBox1_ChonTatCa.AutoSize = true;
            checkBox1_ChonTatCa.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            checkBox1_ChonTatCa.ForeColor = Color.Red;
            checkBox1_ChonTatCa.Location = new Point(3, 2);
            checkBox1_ChonTatCa.Margin = new Padding(3, 2, 3, 2);
            checkBox1_ChonTatCa.Name = "checkBox1_ChonTatCa";
            checkBox1_ChonTatCa.Size = new Size(96, 21);
            checkBox1_ChonTatCa.TabIndex = 3;
            checkBox1_ChonTatCa.Text = "Chọn tất cả";
            checkBox1_ChonTatCa.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 4;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.138248F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19.17329F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5.382953F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 69.30551F));
            tableLayoutPanel2.Controls.Add(pictureBox2, 0, 0);
            tableLayoutPanel2.Controls.Add(label_DuongDanPdf, 3, 0);
            tableLayoutPanel2.Controls.Add(kryptonButton1_ChonDuongDanLuuTepPdf, 2, 0);
            tableLayoutPanel2.Controls.Add(label3, 1, 0);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(3, 65);
            tableLayoutPanel2.Margin = new Padding(3, 2, 3, 2);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Size = new Size(911, 57);
            tableLayoutPanel2.TabIndex = 12;
            // 
            // pictureBox2
            // 
            pictureBox2.Anchor = AnchorStyles.None;
            pictureBox2.Image = (Image)resources.GetObject("pictureBox2.Image");
            pictureBox2.Location = new Point(4, 9);
            pictureBox2.Margin = new Padding(3, 2, 3, 2);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(47, 39);
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox2.TabIndex = 13;
            pictureBox2.TabStop = false;
            // 
            // label_DuongDanPdf
            // 
            label_DuongDanPdf.Anchor = AnchorStyles.Left;
            label_DuongDanPdf.AutoSize = true;
            label_DuongDanPdf.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            label_DuongDanPdf.ForeColor = Color.Red;
            label_DuongDanPdf.Location = new Point(281, 21);
            label_DuongDanPdf.Name = "label_DuongDanPdf";
            label_DuongDanPdf.Size = new Size(179, 15);
            label_DuongDanPdf.TabIndex = 11;
            label_DuongDanPdf.Text = "Chưa chọn thư mục lưu tệp *.pdf";
            label_DuongDanPdf.Click += label_DuongDanPdf_Click;
            // 
            // kryptonButton1_ChonDuongDanLuuTepPdf
            // 
            kryptonButton1_ChonDuongDanLuuTepPdf.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            kryptonButton1_ChonDuongDanLuuTepPdf.Location = new Point(232, 13);
            kryptonButton1_ChonDuongDanLuuTepPdf.Margin = new Padding(3, 2, 3, 2);
            kryptonButton1_ChonDuongDanLuuTepPdf.Name = "kryptonButton1_ChonDuongDanLuuTepPdf";
            kryptonButton1_ChonDuongDanLuuTepPdf.Size = new Size(43, 30);
            kryptonButton1_ChonDuongDanLuuTepPdf.StateCommon.Border.Rounding = 4F;
            kryptonButton1_ChonDuongDanLuuTepPdf.TabIndex = 0;
            kryptonButton1_ChonDuongDanLuuTepPdf.Values.DropDownArrowColor = Color.Empty;
            kryptonButton1_ChonDuongDanLuuTepPdf.Values.Text = "...";
            kryptonButton1_ChonDuongDanLuuTepPdf.Click += kryptonButton1_ChonDuongDanLuuTepPdf_Click;
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Left;
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 10.0173912F, FontStyle.Italic);
            label3.ForeColor = Color.FromArgb(0, 0, 192);
            label3.Location = new Point(58, 19);
            label3.Name = "label3";
            label3.Size = new Size(121, 19);
            label3.TabIndex = 2;
            label3.Text = "Chọn thư mục lưu";
            // 
            // tableLayoutPanel4
            // 
            tableLayoutPanel4.ColumnCount = 4;
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.149194F));
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19.2134838F));
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5.39325857F));
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 69.4382F));
            tableLayoutPanel4.Controls.Add(pictureBox1, 0, 0);
            tableLayoutPanel4.Controls.Add(label_DuongDanExcel, 3, 0);
            tableLayoutPanel4.Controls.Add(kryptonButton1_ChonDuongDanTepExcel, 2, 0);
            tableLayoutPanel4.Controls.Add(label2, 1, 0);
            tableLayoutPanel4.Dock = DockStyle.Fill;
            tableLayoutPanel4.Location = new Point(3, 2);
            tableLayoutPanel4.Margin = new Padding(3, 2, 3, 2);
            tableLayoutPanel4.Name = "tableLayoutPanel4";
            tableLayoutPanel4.RowCount = 1;
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel4.Size = new Size(911, 59);
            tableLayoutPanel4.TabIndex = 11;
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.None;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(4, 10);
            pictureBox1.Margin = new Padding(3, 2, 3, 2);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(46, 39);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 12;
            pictureBox1.TabStop = false;
            // 
            // label_DuongDanExcel
            // 
            label_DuongDanExcel.Anchor = AnchorStyles.Left;
            label_DuongDanExcel.AutoSize = true;
            label_DuongDanExcel.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            label_DuongDanExcel.ForeColor = Color.Red;
            label_DuongDanExcel.Location = new Point(281, 22);
            label_DuongDanExcel.Name = "label_DuongDanExcel";
            label_DuongDanExcel.Size = new Size(84, 15);
            label_DuongDanExcel.TabIndex = 11;
            label_DuongDanExcel.Text = "Chưa chọn tệp";
            label_DuongDanExcel.Click += label_DuongDanExcel_Click;
            // 
            // kryptonButton1_ChonDuongDanTepExcel
            // 
            kryptonButton1_ChonDuongDanTepExcel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            kryptonButton1_ChonDuongDanTepExcel.Location = new Point(232, 14);
            kryptonButton1_ChonDuongDanTepExcel.Margin = new Padding(3, 2, 3, 2);
            kryptonButton1_ChonDuongDanTepExcel.Name = "kryptonButton1_ChonDuongDanTepExcel";
            kryptonButton1_ChonDuongDanTepExcel.Size = new Size(43, 30);
            kryptonButton1_ChonDuongDanTepExcel.StateCommon.Border.Rounding = 4F;
            kryptonButton1_ChonDuongDanTepExcel.TabIndex = 0;
            kryptonButton1_ChonDuongDanTepExcel.Values.DropDownArrowColor = Color.Empty;
            kryptonButton1_ChonDuongDanTepExcel.Values.Text = "";
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Left;
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 10.0173912F, FontStyle.Italic);
            label2.ForeColor = Color.FromArgb(0, 0, 192);
            label2.Location = new Point(58, 20);
            label2.Name = "label2";
            label2.Size = new Size(100, 19);
            label2.TabIndex = 2;
            label2.Text = "Chọn tệp excel";
            // 
            // statusStrip1
            // 
            statusStrip1.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            statusStrip1.BackColor = Color.FromArgb(220, 248, 198);
            statusStrip1.Dock = DockStyle.None;
            statusStrip1.ImageScalingSize = new Size(19, 19);
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripProgressBar1_TienTrinhXuatTep, toolStripStatusLabel1_TongCongTepPdf, toolStripStatusLabel1_DangLoad });
            statusStrip1.Location = new Point(0, 418);
            statusStrip1.MaximumSize = new Size(0, 31);
            statusStrip1.MinimumSize = new Size(0, 31);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(917, 31);
            statusStrip1.SizingGrip = false;
            statusStrip1.TabIndex = 0;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripProgressBar1_TienTrinhXuatTep
            // 
            toolStripProgressBar1_TienTrinhXuatTep.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            toolStripProgressBar1_TienTrinhXuatTep.Name = "toolStripProgressBar1_TienTrinhXuatTep";
            toolStripProgressBar1_TienTrinhXuatTep.Size = new Size(100, 25);
            // 
            // toolStripStatusLabel1_TongCongTepPdf
            // 
            toolStripStatusLabel1_TongCongTepPdf.Image = (Image)resources.GetObject("toolStripStatusLabel1_TongCongTepPdf.Image");
            toolStripStatusLabel1_TongCongTepPdf.Name = "toolStripStatusLabel1_TongCongTepPdf";
            toolStripStatusLabel1_TongCongTepPdf.Size = new Size(87, 26);
            toolStripStatusLabel1_TongCongTepPdf.Text = "Tổng cộng:";
            // 
            // toolStripStatusLabel1_DangLoad
            // 
            toolStripStatusLabel1_DangLoad.Name = "toolStripStatusLabel1_DangLoad";
            toolStripStatusLabel1_DangLoad.Size = new Size(0, 26);
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(checkedListBox1_LietKeTenCacSheet);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Font = new Font("Segoe UI", 12F, FontStyle.Italic, GraphicsUnit.Point, 0);
            groupBox1.ForeColor = Color.FromArgb(0, 0, 192);
            groupBox1.Location = new Point(3, 126);
            groupBox1.Margin = new Padding(3, 2, 3, 2);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(3, 2, 3, 2);
            groupBox1.Size = new Size(911, 231);
            groupBox1.TabIndex = 13;
            groupBox1.TabStop = false;
            groupBox1.Text = "Chọn sheet để tạo tệp (*.pdf)";
            // 
            // checkedListBox1_LietKeTenCacSheet
            // 
            checkedListBox1_LietKeTenCacSheet.Dock = DockStyle.Fill;
            checkedListBox1_LietKeTenCacSheet.Font = new Font("Segoe UI", 9.75F, FontStyle.Italic, GraphicsUnit.Point, 0);
            checkedListBox1_LietKeTenCacSheet.ForeColor = Color.FromArgb(0, 0, 192);
            checkedListBox1_LietKeTenCacSheet.FormattingEnabled = true;
            checkedListBox1_LietKeTenCacSheet.Location = new Point(3, 24);
            checkedListBox1_LietKeTenCacSheet.Margin = new Padding(3, 2, 3, 2);
            checkedListBox1_LietKeTenCacSheet.Name = "checkedListBox1_LietKeTenCacSheet";
            checkedListBox1_LietKeTenCacSheet.Size = new Size(905, 205);
            checkedListBox1_LietKeTenCacSheet.TabIndex = 0;
            // 
            // Form48_XuatTepPdf
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(917, 450);
            Controls.Add(tableLayoutPanel1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 2, 3, 2);
            Name = "Form48_XuatTepPdf";
            Text = "Tạo tệp pdf (Gửi trình ký)";
            Load += Form48_XuatTepPdf_Load;
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            tableLayoutPanel3.ResumeLayout(false);
            tableLayoutPanel3.PerformLayout();
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            tableLayoutPanel4.ResumeLayout(false);
            tableLayoutPanel4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            groupBox1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1_TongCongTepPdf;
        internal Krypton.Toolkit.KryptonButton kryptonButton_XuatTepPdf;
        private TableLayoutPanel tableLayoutPanel4;
        private Label label_DuongDanExcel;
        private Krypton.Toolkit.KryptonButton kryptonButton1_ChonDuongDanTepExcel;
        private Label label2;
        private TableLayoutPanel tableLayoutPanel2;
        private Label label_DuongDanPdf;
        private Krypton.Toolkit.KryptonButton kryptonButton1_ChonDuongDanLuuTepPdf;
        private Label label3;
        private PictureBox pictureBox1;
        private PictureBox pictureBox2;
        private ToolStripProgressBar toolStripProgressBar1_TienTrinhXuatTep;
        private GroupBox groupBox1;
        private CheckedListBox checkedListBox1_LietKeTenCacSheet;
        private ToolTip toolTip1;
        private TableLayoutPanel tableLayoutPanel3;
        private CheckBox checkBox1_ChonTatCa;
        private ToolStripStatusLabel toolStripStatusLabel1_DangLoad;
    }
}