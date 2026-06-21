namespace PhanMemThiDua2026
{
    partial class Form36_TinhPhanLoaiThang
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();

                // 👉 Thêm dòng giải phóng Icon của bạn vào đây:
                _iconStar?.Dispose();
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form36_TinhPhanLoaiThang));
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            toolStripStatusLabel2 = new ToolStripStatusLabel();
            toolStripStatusLabel3 = new ToolStripStatusLabel();
            toolStripStatusLabel4 = new ToolStripStatusLabel();
            toolStripStatusLabel5 = new ToolStripStatusLabel();
            contextMenuStrip1 = new ContextMenuStrip(components);
            lamMoi_ToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripSeparator();
            xuatDuLieu_ToolStripMenuItem = new ToolStripMenuItem();
            troVeThongKe_ToolStripMenuItem = new ToolStripMenuItem();
            tableLayoutPanel1 = new TableLayoutPanel();
            kryptonDataGridView1 = new Krypton.Toolkit.KryptonDataGridView();
            tableLayoutPanel3 = new TableLayoutPanel();
            groupBox5 = new GroupBox();
            tableLayoutPanel7 = new TableLayoutPanel();
            combobox_GiaTriTuanThang_4 = new ComboBox();
            label14 = new Label();
            kryptonButton1_TinhToan = new Krypton.Toolkit.KryptonButton();
            label15 = new Label();
            label16 = new Label();
            combobox_GiaTriTuanThang_3 = new ComboBox();
            combobox_GiaTriTuanThang_2 = new ComboBox();
            combobox_GiaTriTuanThang_1 = new ComboBox();
            label17 = new Label();
            groupBox2 = new GroupBox();
            tableLayoutPanel5 = new TableLayoutPanel();
            comboBox_TimKiemTinhTrang = new ComboBox();
            label1 = new Label();
            textBox_TimKiemTheoTen = new Krypton.Toolkit.KryptonTextBox();
            pictureBox3 = new PictureBox();
            label3 = new Label();
            kryptonButton1_Dong = new Krypton.Toolkit.KryptonButton();
            kryptonButton_LamMoiCacOTimKiem = new Krypton.Toolkit.KryptonButton();
            label5 = new Label();
            comboBox_XepLoaiThiDua = new ComboBox();
            comboBox_TimKiemDonVi = new ComboBox();
            label4 = new Label();
            toolTip1 = new ToolTip(components);
            statusStrip1.SuspendLayout();
            contextMenuStrip1.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)kryptonDataGridView1).BeginInit();
            tableLayoutPanel3.SuspendLayout();
            groupBox5.SuspendLayout();
            tableLayoutPanel7.SuspendLayout();
            groupBox2.SuspendLayout();
            tableLayoutPanel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).BeginInit();
            SuspendLayout();
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1, toolStripStatusLabel2, toolStripStatusLabel3, toolStripStatusLabel4, toolStripStatusLabel5 });
            statusStrip1.Location = new Point(0, 656);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 12, 0);
            statusStrip1.Size = new Size(1264, 25);
            statusStrip1.TabIndex = 0;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Image = (Image)resources.GetObject("toolStripStatusLabel1.Image");
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(138, 20);
            toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // toolStripStatusLabel2
            // 
            toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            toolStripStatusLabel2.Size = new Size(118, 20);
            toolStripStatusLabel2.Text = "toolStripStatusLabel2";
            // 
            // toolStripStatusLabel3
            // 
            toolStripStatusLabel3.Name = "toolStripStatusLabel3";
            toolStripStatusLabel3.Size = new Size(118, 20);
            toolStripStatusLabel3.Text = "toolStripStatusLabel3";
            // 
            // toolStripStatusLabel4
            // 
            toolStripStatusLabel4.Name = "toolStripStatusLabel4";
            toolStripStatusLabel4.Size = new Size(118, 20);
            toolStripStatusLabel4.Text = "toolStripStatusLabel4";
            // 
            // toolStripStatusLabel5
            // 
            toolStripStatusLabel5.Name = "toolStripStatusLabel5";
            toolStripStatusLabel5.Size = new Size(118, 20);
            toolStripStatusLabel5.Text = "toolStripStatusLabel5";
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Font = new Font("Segoe UI", 9F);
            contextMenuStrip1.ImageScalingSize = new Size(20, 20);
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { lamMoi_ToolStripMenuItem, toolStripMenuItem1, xuatDuLieu_ToolStripMenuItem, troVeThongKe_ToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(182, 88);
            // 
            // lamMoi_ToolStripMenuItem
            // 
            lamMoi_ToolStripMenuItem.Image = (Image)resources.GetObject("lamMoi_ToolStripMenuItem.Image");
            lamMoi_ToolStripMenuItem.Name = "lamMoi_ToolStripMenuItem";
            lamMoi_ToolStripMenuItem.ShortcutKeys = Keys.F5;
            lamMoi_ToolStripMenuItem.Size = new Size(181, 26);
            lamMoi_ToolStripMenuItem.Text = "Làm mới";
            lamMoi_ToolStripMenuItem.Click += lamMoi_ToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(178, 6);
            // 
            // xuatDuLieu_ToolStripMenuItem
            // 
            xuatDuLieu_ToolStripMenuItem.Image = (Image)resources.GetObject("xuatDuLieu_ToolStripMenuItem.Image");
            xuatDuLieu_ToolStripMenuItem.Name = "xuatDuLieu_ToolStripMenuItem";
            xuatDuLieu_ToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.E;
            xuatDuLieu_ToolStripMenuItem.Size = new Size(181, 26);
            xuatDuLieu_ToolStripMenuItem.Text = "Xuất dữ liệu";
            xuatDuLieu_ToolStripMenuItem.Click += xuatDuLieu_ToolStripMenuItem_Click;
            // 
            // troVeThongKe_ToolStripMenuItem
            // 
            troVeThongKe_ToolStripMenuItem.Image = (Image)resources.GetObject("troVeThongKe_ToolStripMenuItem.Image");
            troVeThongKe_ToolStripMenuItem.Name = "troVeThongKe_ToolStripMenuItem";
            troVeThongKe_ToolStripMenuItem.Size = new Size(181, 26);
            troVeThongKe_ToolStripMenuItem.Text = "Trở về Thống kê";
            troVeThongKe_ToolStripMenuItem.Click += troVeThongKe_ToolStripMenuItem_Click;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(kryptonDataGridView1, 0, 1);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel3, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 22.8658543F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 77.13415F));
            tableLayoutPanel1.Size = new Size(1264, 656);
            tableLayoutPanel1.TabIndex = 2;
            // 
            // kryptonDataGridView1
            // 
            kryptonDataGridView1.BorderStyle = BorderStyle.None;
            kryptonDataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            kryptonDataGridView1.ContextMenuStrip = contextMenuStrip1;
            kryptonDataGridView1.Dock = DockStyle.Fill;
            kryptonDataGridView1.Location = new Point(2, 153);
            kryptonDataGridView1.Margin = new Padding(2, 3, 2, 3);
            kryptonDataGridView1.Name = "kryptonDataGridView1";
            kryptonDataGridView1.RowHeadersWidth = 53;
            kryptonDataGridView1.Size = new Size(1260, 500);
            kryptonDataGridView1.TabIndex = 10;
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.ColumnCount = 1;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.Controls.Add(groupBox5, 0, 1);
            tableLayoutPanel3.Controls.Add(groupBox2, 0, 0);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new Point(2, 3);
            tableLayoutPanel3.Margin = new Padding(2, 3, 2, 3);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 2;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 51.2987022F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 48.7012978F));
            tableLayoutPanel3.Size = new Size(1260, 144);
            tableLayoutPanel3.TabIndex = 9;
            // 
            // groupBox5
            // 
            groupBox5.Controls.Add(tableLayoutPanel7);
            groupBox5.Dock = DockStyle.Fill;
            groupBox5.Font = new Font("Segoe UI", 9.216F, FontStyle.Italic, GraphicsUnit.Point, 0);
            groupBox5.ForeColor = Color.Red;
            groupBox5.Location = new Point(2, 76);
            groupBox5.Margin = new Padding(2, 3, 2, 3);
            groupBox5.Name = "groupBox5";
            groupBox5.Padding = new Padding(2, 3, 2, 3);
            groupBox5.Size = new Size(1256, 65);
            groupBox5.TabIndex = 37;
            groupBox5.TabStop = false;
            groupBox5.Text = "2. Chọn tuần/tháng để tính kết quả phân loại";
            // 
            // tableLayoutPanel7
            // 
            tableLayoutPanel7.ColumnCount = 9;
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10.9756107F));
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9.92F));
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F));
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9.44F));
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.48F));
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10.16F));
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11.6F));
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9.6F));
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13.68F));
            tableLayoutPanel7.Controls.Add(combobox_GiaTriTuanThang_4, 7, 0);
            tableLayoutPanel7.Controls.Add(label14, 6, 0);
            tableLayoutPanel7.Controls.Add(kryptonButton1_TinhToan, 8, 0);
            tableLayoutPanel7.Controls.Add(label15, 4, 0);
            tableLayoutPanel7.Controls.Add(label16, 2, 0);
            tableLayoutPanel7.Controls.Add(combobox_GiaTriTuanThang_3, 5, 0);
            tableLayoutPanel7.Controls.Add(combobox_GiaTriTuanThang_2, 3, 0);
            tableLayoutPanel7.Controls.Add(combobox_GiaTriTuanThang_1, 1, 0);
            tableLayoutPanel7.Controls.Add(label17, 0, 0);
            tableLayoutPanel7.Dock = DockStyle.Fill;
            tableLayoutPanel7.Location = new Point(2, 20);
            tableLayoutPanel7.Margin = new Padding(2, 3, 2, 3);
            tableLayoutPanel7.Name = "tableLayoutPanel7";
            tableLayoutPanel7.RowCount = 1;
            tableLayoutPanel7.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel7.Size = new Size(1252, 42);
            tableLayoutPanel7.TabIndex = 0;
            // 
            // combobox_GiaTriTuanThang_4
            // 
            combobox_GiaTriTuanThang_4.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            combobox_GiaTriTuanThang_4.DropDownStyle = ComboBoxStyle.DropDownList;
            combobox_GiaTriTuanThang_4.FormattingEnabled = true;
            combobox_GiaTriTuanThang_4.Location = new Point(959, 9);
            combobox_GiaTriTuanThang_4.Margin = new Padding(2, 3, 2, 3);
            combobox_GiaTriTuanThang_4.Name = "combobox_GiaTriTuanThang_4";
            combobox_GiaTriTuanThang_4.Size = new Size(116, 23);
            combobox_GiaTriTuanThang_4.TabIndex = 12;
            // 
            // label14
            // 
            label14.Anchor = AnchorStyles.Right;
            label14.AutoSize = true;
            label14.Font = new Font("Segoe UI", 9.216F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label14.ForeColor = Color.FromArgb(0, 0, 192);
            label14.Location = new Point(901, 12);
            label14.Margin = new Padding(2, 0, 2, 0);
            label14.Name = "label14";
            label14.Size = new Size(54, 17);
            label14.TabIndex = 11;
            label14.Text = "Giá trị 3";
            // 
            // kryptonButton1_TinhToan
            // 
            kryptonButton1_TinhToan.Anchor = AnchorStyles.None;
            kryptonButton1_TinhToan.Location = new Point(1099, 6);
            kryptonButton1_TinhToan.Margin = new Padding(2, 3, 2, 3);
            kryptonButton1_TinhToan.Name = "kryptonButton1_TinhToan";
            kryptonButton1_TinhToan.Size = new Size(131, 30);
            kryptonButton1_TinhToan.StateCommon.Border.Rounding = 4F;
            kryptonButton1_TinhToan.StateCommon.Content.ShortText.Font = new Font("Segoe UI", 9.216F);
            kryptonButton1_TinhToan.TabIndex = 38;
            kryptonButton1_TinhToan.Values.DropDownArrowColor = Color.Empty;
            kryptonButton1_TinhToan.Values.Image = (Image)resources.GetObject("kryptonButton1_TinhToan.Values.Image");
            kryptonButton1_TinhToan.Values.Text = "Tính phân loại";
            kryptonButton1_TinhToan.Click += kryptonButton1_TinhToan_Click;
            // 
            // label15
            // 
            label15.Anchor = AnchorStyles.Right;
            label15.AutoSize = true;
            label15.Font = new Font("Segoe UI", 9.216F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label15.ForeColor = Color.FromArgb(0, 0, 192);
            label15.Location = new Point(629, 12);
            label15.Margin = new Padding(2, 0, 2, 0);
            label15.Name = "label15";
            label15.Size = new Size(54, 17);
            label15.TabIndex = 10;
            label15.Text = "Giá trị 3";
            // 
            // label16
            // 
            label16.Anchor = AnchorStyles.Right;
            label16.AutoSize = true;
            label16.Font = new Font("Segoe UI", 9.216F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label16.ForeColor = Color.FromArgb(0, 0, 192);
            label16.Location = new Point(355, 12);
            label16.Margin = new Padding(2, 0, 2, 0);
            label16.Name = "label16";
            label16.Size = new Size(54, 17);
            label16.TabIndex = 9;
            label16.Text = "Giá trị 2";
            // 
            // combobox_GiaTriTuanThang_3
            // 
            combobox_GiaTriTuanThang_3.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            combobox_GiaTriTuanThang_3.DropDownStyle = ComboBoxStyle.DropDownList;
            combobox_GiaTriTuanThang_3.FormattingEnabled = true;
            combobox_GiaTriTuanThang_3.Location = new Point(687, 9);
            combobox_GiaTriTuanThang_3.Margin = new Padding(2, 3, 2, 3);
            combobox_GiaTriTuanThang_3.Name = "combobox_GiaTriTuanThang_3";
            combobox_GiaTriTuanThang_3.Size = new Size(123, 23);
            combobox_GiaTriTuanThang_3.TabIndex = 8;
            // 
            // combobox_GiaTriTuanThang_2
            // 
            combobox_GiaTriTuanThang_2.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            combobox_GiaTriTuanThang_2.DropDownStyle = ComboBoxStyle.DropDownList;
            combobox_GiaTriTuanThang_2.FormattingEnabled = true;
            combobox_GiaTriTuanThang_2.Items.AddRange(new object[] { "Tất cả", "Loại 1", "Loại 2", "Loại 3", "Loại 4", "Không PL" });
            combobox_GiaTriTuanThang_2.Location = new Point(413, 9);
            combobox_GiaTriTuanThang_2.Margin = new Padding(2, 3, 2, 3);
            combobox_GiaTriTuanThang_2.Name = "combobox_GiaTriTuanThang_2";
            combobox_GiaTriTuanThang_2.Size = new Size(114, 23);
            combobox_GiaTriTuanThang_2.TabIndex = 6;
            // 
            // combobox_GiaTriTuanThang_1
            // 
            combobox_GiaTriTuanThang_1.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            combobox_GiaTriTuanThang_1.DropDownStyle = ComboBoxStyle.DropDownList;
            combobox_GiaTriTuanThang_1.FormattingEnabled = true;
            combobox_GiaTriTuanThang_1.Items.AddRange(new object[] { "Tất cả", "BCH", "TTM,D2", "TCT,D2", "THC,D2", "C1,D2", "C2,D2", "C3,D2", "CCG,D2" });
            combobox_GiaTriTuanThang_1.Location = new Point(139, 9);
            combobox_GiaTriTuanThang_1.Margin = new Padding(2, 3, 2, 3);
            combobox_GiaTriTuanThang_1.Name = "combobox_GiaTriTuanThang_1";
            combobox_GiaTriTuanThang_1.Size = new Size(120, 23);
            combobox_GiaTriTuanThang_1.TabIndex = 4;
            // 
            // label17
            // 
            label17.Anchor = AnchorStyles.Right;
            label17.AutoSize = true;
            label17.Font = new Font("Segoe UI", 9.216F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label17.ForeColor = Color.FromArgb(0, 0, 192);
            label17.Location = new Point(81, 12);
            label17.Margin = new Padding(2, 0, 2, 0);
            label17.Name = "label17";
            label17.Size = new Size(54, 17);
            label17.TabIndex = 3;
            label17.Text = "Giá trị 1";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(tableLayoutPanel5);
            groupBox2.Dock = DockStyle.Fill;
            groupBox2.Font = new Font("Segoe UI", 9.216F, FontStyle.Italic, GraphicsUnit.Point, 0);
            groupBox2.ForeColor = Color.Red;
            groupBox2.Location = new Point(2, 3);
            groupBox2.Margin = new Padding(2, 3, 2, 3);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(2, 3, 2, 3);
            groupBox2.Size = new Size(1256, 67);
            groupBox2.TabIndex = 35;
            groupBox2.TabStop = false;
            groupBox2.Text = "1. Nhập tìm kiếm";
            // 
            // tableLayoutPanel5
            // 
            tableLayoutPanel5.ColumnCount = 11;
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 3.36F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 7.36F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15.36F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 7.92F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10.16F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6.4F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9.28F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 8.56F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.48F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5.68F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13.28F));
            tableLayoutPanel5.Controls.Add(comboBox_TimKiemTinhTrang, 4, 0);
            tableLayoutPanel5.Controls.Add(label1, 3, 0);
            tableLayoutPanel5.Controls.Add(textBox_TimKiemTheoTen, 2, 0);
            tableLayoutPanel5.Controls.Add(pictureBox3, 0, 0);
            tableLayoutPanel5.Controls.Add(label3, 1, 0);
            tableLayoutPanel5.Controls.Add(kryptonButton1_Dong, 10, 0);
            tableLayoutPanel5.Controls.Add(kryptonButton_LamMoiCacOTimKiem, 9, 0);
            tableLayoutPanel5.Controls.Add(label5, 7, 0);
            tableLayoutPanel5.Controls.Add(comboBox_XepLoaiThiDua, 8, 0);
            tableLayoutPanel5.Controls.Add(comboBox_TimKiemDonVi, 6, 0);
            tableLayoutPanel5.Controls.Add(label4, 5, 0);
            tableLayoutPanel5.Dock = DockStyle.Fill;
            tableLayoutPanel5.Font = new Font("Segoe UI", 9.216F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tableLayoutPanel5.Location = new Point(2, 20);
            tableLayoutPanel5.Name = "tableLayoutPanel5";
            tableLayoutPanel5.RowCount = 1;
            tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel5.Size = new Size(1252, 44);
            tableLayoutPanel5.TabIndex = 2;
            // 
            // comboBox_TimKiemTinhTrang
            // 
            comboBox_TimKiemTinhTrang.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            comboBox_TimKiemTinhTrang.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_TimKiemTinhTrang.FormattingEnabled = true;
            comboBox_TimKiemTinhTrang.Location = new Point(428, 10);
            comboBox_TimKiemTinhTrang.Name = "comboBox_TimKiemTinhTrang";
            comboBox_TimKiemTinhTrang.Size = new Size(121, 23);
            comboBox_TimKiemTinhTrang.TabIndex = 32;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9.216F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.FromArgb(0, 0, 192);
            label1.Location = new Point(356, 13);
            label1.Name = "label1";
            label1.Size = new Size(66, 17);
            label1.TabIndex = 31;
            label1.Text = "Tình trạng";
            // 
            // textBox_TimKiemTheoTen
            // 
            textBox_TimKiemTheoTen.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            textBox_TimKiemTheoTen.Location = new Point(136, 7);
            textBox_TimKiemTheoTen.Margin = new Padding(2, 3, 2, 3);
            textBox_TimKiemTheoTen.Name = "textBox_TimKiemTheoTen";
            textBox_TimKiemTheoTen.Size = new Size(188, 29);
            textBox_TimKiemTheoTen.StateCommon.Border.Rounding = 8F;
            textBox_TimKiemTheoTen.StateCommon.Border.Width = 1;
            textBox_TimKiemTheoTen.TabIndex = 30;
            // 
            // pictureBox3
            // 
            pictureBox3.Anchor = AnchorStyles.None;
            pictureBox3.Image = (Image)resources.GetObject("pictureBox3.Image");
            pictureBox3.Location = new Point(4, 7);
            pictureBox3.Margin = new Padding(2, 3, 2, 3);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new Size(33, 29);
            pictureBox3.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox3.TabIndex = 29;
            pictureBox3.TabStop = false;
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Right;
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 9.216F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label3.ForeColor = Color.FromArgb(0, 0, 192);
            label3.Location = new Point(69, 13);
            label3.Name = "label3";
            label3.Size = new Size(62, 17);
            label3.TabIndex = 1;
            label3.Text = "Họ và tên";
            label3.TextAlign = ContentAlignment.MiddleRight;
            // 
            // kryptonButton1_Dong
            // 
            kryptonButton1_Dong.Anchor = AnchorStyles.None;
            kryptonButton1_Dong.Location = new Point(1107, 7);
            kryptonButton1_Dong.Margin = new Padding(2, 3, 2, 3);
            kryptonButton1_Dong.Name = "kryptonButton1_Dong";
            kryptonButton1_Dong.Size = new Size(119, 30);
            kryptonButton1_Dong.StateCommon.Border.Rounding = 4F;
            kryptonButton1_Dong.StateCommon.Content.ShortText.Font = new Font("Segoe UI", 9.216F);
            kryptonButton1_Dong.TabIndex = 27;
            kryptonButton1_Dong.Values.DropDownArrowColor = Color.Empty;
            kryptonButton1_Dong.Values.Image = (Image)resources.GetObject("kryptonButton1_Dong.Values.Image");
            kryptonButton1_Dong.Values.Text = "Đóng";
            kryptonButton1_Dong.Click += kryptonButton1_Dong_Click;
            // 
            // kryptonButton_LamMoiCacOTimKiem
            // 
            kryptonButton_LamMoiCacOTimKiem.Anchor = AnchorStyles.None;
            kryptonButton_LamMoiCacOTimKiem.Location = new Point(1024, 9);
            kryptonButton_LamMoiCacOTimKiem.Name = "kryptonButton_LamMoiCacOTimKiem";
            kryptonButton_LamMoiCacOTimKiem.Size = new Size(44, 26);
            kryptonButton_LamMoiCacOTimKiem.StateCommon.Border.Rounding = 4F;
            kryptonButton_LamMoiCacOTimKiem.StateTracking.Border.Rounding = 4F;
            kryptonButton_LamMoiCacOTimKiem.TabIndex = 3;
            kryptonButton_LamMoiCacOTimKiem.Values.DropDownArrowColor = Color.Empty;
            kryptonButton_LamMoiCacOTimKiem.Values.Image = (Image)resources.GetObject("kryptonButton_LamMoiCacOTimKiem.Values.Image");
            kryptonButton_LamMoiCacOTimKiem.Values.Text = "";
            kryptonButton_LamMoiCacOTimKiem.Click += kryptonButton_LamMoiCacOTimKiem_Click;
            // 
            // label5
            // 
            label5.Anchor = AnchorStyles.Right;
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 9.216F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label5.ForeColor = Color.FromArgb(0, 0, 192);
            label5.Location = new Point(799, 13);
            label5.Name = "label5";
            label5.Size = new Size(53, 17);
            label5.TabIndex = 3;
            label5.Text = "Xếp loại";
            // 
            // comboBox_XepLoaiThiDua
            // 
            comboBox_XepLoaiThiDua.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            comboBox_XepLoaiThiDua.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_XepLoaiThiDua.FormattingEnabled = true;
            comboBox_XepLoaiThiDua.Items.AddRange(new object[] { "Tất cả", "Loại 1", "Loại 2", "Loại 3", "Loại 4", "Không PL" });
            comboBox_XepLoaiThiDua.Location = new Point(858, 10);
            comboBox_XepLoaiThiDua.Name = "comboBox_XepLoaiThiDua";
            comboBox_XepLoaiThiDua.Size = new Size(150, 23);
            comboBox_XepLoaiThiDua.TabIndex = 2;
            // 
            // comboBox_TimKiemDonVi
            // 
            comboBox_TimKiemDonVi.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            comboBox_TimKiemDonVi.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_TimKiemDonVi.FormattingEnabled = true;
            comboBox_TimKiemDonVi.Items.AddRange(new object[] { "Tất cả", "BCH", "TTM,D2", "TCT,D2", "THC,D2", "C1,D2", "C2,D2", "C3,D2", "CCG,D2" });
            comboBox_TimKiemDonVi.Location = new Point(635, 10);
            comboBox_TimKiemDonVi.Name = "comboBox_TimKiemDonVi";
            comboBox_TimKiemDonVi.Size = new Size(110, 23);
            comboBox_TimKiemDonVi.TabIndex = 1;
            // 
            // label4
            // 
            label4.Anchor = AnchorStyles.Right;
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 9.216F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label4.ForeColor = Color.FromArgb(0, 0, 192);
            label4.Location = new Point(585, 13);
            label4.Name = "label4";
            label4.Size = new Size(44, 17);
            label4.TabIndex = 2;
            label4.Text = "Đơn vị";
            // 
            // Form36_TinhPhanLoaiThang
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1264, 681);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(statusStrip1);
            Name = "Form36_TinhPhanLoaiThang";
            Text = "Tinh toán phân loại thi đua tháng";
            Load += Form36_TinhPhanLoaiThang_Load;
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            contextMenuStrip1.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)kryptonDataGridView1).EndInit();
            tableLayoutPanel3.ResumeLayout(false);
            groupBox5.ResumeLayout(false);
            tableLayoutPanel7.ResumeLayout(false);
            tableLayoutPanel7.PerformLayout();
            groupBox2.ResumeLayout(false);
            tableLayoutPanel5.ResumeLayout(false);
            tableLayoutPanel5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private StatusStrip statusStrip1;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem xuatDuLieu_ToolStripMenuItem;
        private ToolStripMenuItem lamMoi_ToolStripMenuItem;
        private TableLayoutPanel tableLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel3;
        internal Krypton.Toolkit.KryptonButton kryptonButton1_Dong;
        private GroupBox groupBox2;
        private GroupBox groupBox5;
        private TableLayoutPanel tableLayoutPanel7;
        private ComboBox combobox_GiaTriTuanThang_4;
        private Label label14;
        private Label label15;
        private Label label16;
        private ComboBox combobox_GiaTriTuanThang_3;
        private ComboBox combobox_GiaTriTuanThang_2;
        private ComboBox combobox_GiaTriTuanThang_1;
        private Label label17;
        internal Krypton.Toolkit.KryptonButton kryptonButton1_TinhToan;
        private TableLayoutPanel tableLayoutPanel5;
        private ComboBox comboBox_XepLoaiThiDua;
        private Label label3;
        private Label label4;
        private Label label5;
        private ComboBox comboBox_TimKiemDonVi;
        private Krypton.Toolkit.KryptonButton kryptonButton_LamMoiCacOTimKiem;
        private PictureBox pictureBox3;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripStatusLabel toolStripStatusLabel2;
        private ToolStripStatusLabel toolStripStatusLabel3;
        private ToolStripStatusLabel toolStripStatusLabel4;
        private ToolStripStatusLabel toolStripStatusLabel5;
        private Krypton.Toolkit.KryptonTextBox textBox_TimKiemTheoTen;
        private ToolStripMenuItem troVeThongKe_ToolStripMenuItem;
        private Krypton.Toolkit.KryptonDataGridView kryptonDataGridView1;
        private ComboBox comboBox_TimKiemTinhTrang;
        private Label label1;
        private ToolTip toolTip1;
        private ToolStripSeparator toolStripMenuItem1;
    }
}