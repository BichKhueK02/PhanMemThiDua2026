namespace PhanMemThiDua2026
{
    partial class Form62_ChinhSuaDataNhomTuan
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form62_ChinhSuaDataNhomTuan));
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            tableLayoutPanel_Khung = new TableLayoutPanel();
            tableLayoutPanel_DauTrang = new TableLayoutPanel();
            pictureBox1 = new PictureBox();
            label_HoTen = new NhanMotDong();
            label_SoHieu = new NhanMotDong();
            label_DonVi = new NhanMotDong();
            roundedPanel_Luoi = new RoundedPanel();
            dataGridView_LichXet = new DataGridView();
            Cot_ThangXet = new DataGridViewTextBoxColumn();
            Cot_Tuan1 = new DataGridViewComboBoxColumn();
            Cot_Tuan2 = new DataGridViewComboBoxColumn();
            Cot_Tuan3 = new DataGridViewComboBoxColumn();
            Cot_Tuan4 = new DataGridViewComboBoxColumn();
            Cot_KetQua = new DataGridViewComboBoxColumn();
            label_ChuThich = new Label();
            tableLayoutPanel_Nut = new TableLayoutPanel();
            label_HuongDan = new Label();
            kryptonButton_CapNhat = new Krypton.Toolkit.KryptonButton();
            tableLayoutPanel_Khung.SuspendLayout();
            tableLayoutPanel_DauTrang.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            roundedPanel_Luoi.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_LichXet).BeginInit();
            tableLayoutPanel_Nut.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel_Khung
            // 
            tableLayoutPanel_Khung.BackColor = Color.White;
            tableLayoutPanel_Khung.ColumnCount = 1;
            tableLayoutPanel_Khung.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel_Khung.Controls.Add(tableLayoutPanel_DauTrang, 0, 0);
            tableLayoutPanel_Khung.Controls.Add(roundedPanel_Luoi, 0, 1);
            tableLayoutPanel_Khung.Controls.Add(label_ChuThich, 0, 2);
            tableLayoutPanel_Khung.Controls.Add(tableLayoutPanel_Nut, 0, 3);
            tableLayoutPanel_Khung.Dock = DockStyle.Fill;
            tableLayoutPanel_Khung.Location = new Point(0, 0);
            tableLayoutPanel_Khung.Name = "tableLayoutPanel_Khung";
            tableLayoutPanel_Khung.Padding = new Padding(16, 12, 16, 12);
            tableLayoutPanel_Khung.RowCount = 4;
            tableLayoutPanel_Khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            tableLayoutPanel_Khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel_Khung.RowStyles.Add(new RowStyle());
            tableLayoutPanel_Khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            tableLayoutPanel_Khung.Size = new Size(1121, 401);
            tableLayoutPanel_Khung.TabIndex = 0;
            // 
            // tableLayoutPanel_DauTrang
            // 
            tableLayoutPanel_DauTrang.ColumnCount = 4;
            tableLayoutPanel_DauTrang.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            tableLayoutPanel_DauTrang.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            tableLayoutPanel_DauTrang.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel_DauTrang.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            tableLayoutPanel_DauTrang.Controls.Add(pictureBox1, 0, 0);
            tableLayoutPanel_DauTrang.Controls.Add(label_HoTen, 1, 0);
            tableLayoutPanel_DauTrang.Controls.Add(label_SoHieu, 2, 0);
            tableLayoutPanel_DauTrang.Controls.Add(label_DonVi, 3, 0);
            tableLayoutPanel_DauTrang.Dock = DockStyle.Fill;
            tableLayoutPanel_DauTrang.Location = new Point(16, 12);
            tableLayoutPanel_DauTrang.Margin = new Padding(0, 0, 0, 8);
            tableLayoutPanel_DauTrang.Name = "tableLayoutPanel_DauTrang";
            tableLayoutPanel_DauTrang.RowCount = 1;
            tableLayoutPanel_DauTrang.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel_DauTrang.Size = new Size(1089, 62);
            tableLayoutPanel_DauTrang.TabIndex = 0;
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.None;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(5, 6);
            pictureBox1.Margin = new Padding(0);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(82, 49);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // label_HoTen
            // 
            label_HoTen.Dock = DockStyle.Fill;
            label_HoTen.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            label_HoTen.ForeColor = Color.Green;
            label_HoTen.Location = new Point(96, 0);
            label_HoTen.Margin = new Padding(4, 0, 4, 0);
            label_HoTen.Name = "label_HoTen";
            label_HoTen.Size = new Size(440, 62);
            label_HoTen.TabIndex = 1;
            label_HoTen.Text = "HoVaTen";
            label_HoTen.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label_SoHieu
            // 
            label_SoHieu.Dock = DockStyle.Fill;
            label_SoHieu.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            label_SoHieu.ForeColor = Color.Green;
            label_SoHieu.Location = new Point(544, 0);
            label_SoHieu.Margin = new Padding(4, 0, 4, 0);
            label_SoHieu.Name = "label_SoHieu";
            label_SoHieu.Size = new Size(191, 62);
            label_SoHieu.TabIndex = 2;
            label_SoHieu.Text = "SoHieu";
            label_SoHieu.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label_DonVi
            // 
            label_DonVi.Dock = DockStyle.Fill;
            label_DonVi.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            label_DonVi.ForeColor = Color.Green;
            label_DonVi.Location = new Point(743, 0);
            label_DonVi.Margin = new Padding(4, 0, 4, 0);
            label_DonVi.Name = "label_DonVi";
            label_DonVi.Size = new Size(342, 62);
            label_DonVi.TabIndex = 3;
            label_DonVi.Text = "DonVi";
            label_DonVi.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // roundedPanel_Luoi
            // 
            roundedPanel_Luoi.BackColor = Color.White;
            roundedPanel_Luoi.BoGocControlCon = true;
            roundedPanel_Luoi.BorderColor = Color.FromArgb(221, 214, 254);
            roundedPanel_Luoi.BorderWidth = 1;
            roundedPanel_Luoi.Controls.Add(dataGridView_LichXet);
            roundedPanel_Luoi.Dock = DockStyle.Fill;
            roundedPanel_Luoi.FillColor = Color.White;
            roundedPanel_Luoi.Location = new Point(16, 82);
            roundedPanel_Luoi.Margin = new Padding(0, 0, 0, 6);
            roundedPanel_Luoi.Name = "roundedPanel_Luoi";
            roundedPanel_Luoi.Padding = new Padding(1);
            roundedPanel_Luoi.Radius = 10;
            roundedPanel_Luoi.Size = new Size(1089, 232);
            roundedPanel_Luoi.TabIndex = 1;
            // 
            // dataGridView_LichXet
            // 
            dataGridView_LichXet.AllowUserToAddRows = false;
            dataGridView_LichXet.AllowUserToDeleteRows = false;
            dataGridView_LichXet.AllowUserToResizeColumns = false;
            dataGridView_LichXet.AllowUserToResizeRows = false;
            dataGridView_LichXet.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView_LichXet.BackgroundColor = Color.White;
            dataGridView_LichXet.BorderStyle = BorderStyle.None;
            dataGridView_LichXet.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(237, 233, 254);
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = Color.FromArgb(91, 33, 182);
            dataGridViewCellStyle1.SelectionBackColor = Color.FromArgb(237, 233, 254);
            dataGridViewCellStyle1.SelectionForeColor = Color.FromArgb(91, 33, 182);
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dataGridView_LichXet.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dataGridView_LichXet.ColumnHeadersHeight = 42;
            dataGridView_LichXet.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridView_LichXet.Columns.AddRange(new DataGridViewColumn[] { Cot_ThangXet, Cot_Tuan1, Cot_Tuan2, Cot_Tuan3, Cot_Tuan4, Cot_KetQua });
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = Color.White;
            dataGridViewCellStyle3.Font = new Font("Segoe UI", 10F);
            dataGridViewCellStyle3.ForeColor = Color.Black;
            dataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(232, 244, 253);
            dataGridViewCellStyle3.SelectionForeColor = Color.FromArgb(0, 102, 204);
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
            dataGridView_LichXet.DefaultCellStyle = dataGridViewCellStyle3;
            dataGridView_LichXet.Dock = DockStyle.Fill;
            dataGridView_LichXet.EditMode = DataGridViewEditMode.EditOnEnter;
            dataGridView_LichXet.EnableHeadersVisualStyles = false;
            dataGridView_LichXet.GridColor = Color.FromArgb(221, 214, 254);
            dataGridView_LichXet.Location = new Point(1, 1);
            dataGridView_LichXet.Margin = new Padding(0);
            dataGridView_LichXet.MultiSelect = false;
            dataGridView_LichXet.Name = "dataGridView_LichXet";
            dataGridView_LichXet.RowHeadersVisible = false;
            dataGridView_LichXet.RowTemplate.Height = 32;
            dataGridView_LichXet.ScrollBars = ScrollBars.None;
            dataGridView_LichXet.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView_LichXet.Size = new Size(1087, 230);
            dataGridView_LichXet.TabIndex = 0;
            // 
            // Cot_ThangXet
            // 
            dataGridViewCellStyle2.BackColor = Color.FromArgb(250, 249, 255);
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 10F, FontStyle.Italic);
            dataGridViewCellStyle2.ForeColor = Color.FromArgb(55, 65, 81);
            dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(250, 249, 255);
            dataGridViewCellStyle2.SelectionForeColor = Color.FromArgb(55, 65, 81);
            Cot_ThangXet.DefaultCellStyle = dataGridViewCellStyle2;
            Cot_ThangXet.FillWeight = 150F;
            Cot_ThangXet.HeaderText = "Tháng xét";
            Cot_ThangXet.Name = "Cot_ThangXet";
            Cot_ThangXet.ReadOnly = true;
            Cot_ThangXet.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // Cot_Tuan1
            // 
            Cot_Tuan1.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
            Cot_Tuan1.FlatStyle = FlatStyle.Flat;
            Cot_Tuan1.HeaderText = "Tuần 1";
            Cot_Tuan1.Items.AddRange(new object[] { "", "Loại 1", "Loại 2", "Loại 3", "Loại 4" });
            Cot_Tuan1.Name = "Cot_Tuan1";
            // 
            // Cot_Tuan2
            // 
            Cot_Tuan2.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
            Cot_Tuan2.FlatStyle = FlatStyle.Flat;
            Cot_Tuan2.HeaderText = "Tuần 2";
            Cot_Tuan2.Items.AddRange(new object[] { "", "Loại 1", "Loại 2", "Loại 3", "Loại 4" });
            Cot_Tuan2.Name = "Cot_Tuan2";
            // 
            // Cot_Tuan3
            // 
            Cot_Tuan3.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
            Cot_Tuan3.FlatStyle = FlatStyle.Flat;
            Cot_Tuan3.HeaderText = "Tuần 3";
            Cot_Tuan3.Items.AddRange(new object[] { "", "Loại 1", "Loại 2", "Loại 3", "Loại 4" });
            Cot_Tuan3.Name = "Cot_Tuan3";
            // 
            // Cot_Tuan4
            // 
            Cot_Tuan4.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
            Cot_Tuan4.FlatStyle = FlatStyle.Flat;
            Cot_Tuan4.HeaderText = "Tuần 4";
            Cot_Tuan4.Items.AddRange(new object[] { "", "Loại 1", "Loại 2", "Loại 3", "Loại 4" });
            Cot_Tuan4.Name = "Cot_Tuan4";
            // 
            // Cot_KetQua
            // 
            Cot_KetQua.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
            Cot_KetQua.FillWeight = 120F;
            Cot_KetQua.FlatStyle = FlatStyle.Flat;
            Cot_KetQua.HeaderText = "Kết quả tháng";
            Cot_KetQua.Items.AddRange(new object[] { "", "Loại 1", "Loại 2", "Loại 3", "Loại 4" });
            Cot_KetQua.Name = "Cot_KetQua";
            // 
            // label_ChuThich
            // 
            label_ChuThich.AutoSize = true;
            label_ChuThich.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            label_ChuThich.ForeColor = Color.DimGray;
            label_ChuThich.Location = new Point(18, 320);
            label_ChuThich.Margin = new Padding(2, 0, 0, 6);
            label_ChuThich.Name = "label_ChuThich";
            label_ChuThich.Size = new Size(0, 15);
            label_ChuThich.TabIndex = 2;
            // 
            // tableLayoutPanel_Nut
            // 
            tableLayoutPanel_Nut.ColumnCount = 2;
            tableLayoutPanel_Nut.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel_Nut.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel_Nut.Controls.Add(label_HuongDan, 0, 0);
            tableLayoutPanel_Nut.Controls.Add(kryptonButton_CapNhat, 1, 0);
            tableLayoutPanel_Nut.Dock = DockStyle.Fill;
            tableLayoutPanel_Nut.Location = new Point(16, 341);
            tableLayoutPanel_Nut.Margin = new Padding(0);
            tableLayoutPanel_Nut.Name = "tableLayoutPanel_Nut";
            tableLayoutPanel_Nut.RowCount = 1;
            tableLayoutPanel_Nut.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel_Nut.Size = new Size(1089, 48);
            tableLayoutPanel_Nut.TabIndex = 3;
            // 
            // label_HuongDan
            // 
            label_HuongDan.Anchor = AnchorStyles.Left;
            label_HuongDan.AutoSize = true;
            label_HuongDan.Font = new Font("Segoe UI", 9F);
            label_HuongDan.ForeColor = Color.SteelBlue;
            label_HuongDan.Location = new Point(2, 16);
            label_HuongDan.Margin = new Padding(2, 0, 0, 0);
            label_HuongDan.Name = "label_HuongDan";
            label_HuongDan.Size = new Size(312, 15);
            label_HuongDan.TabIndex = 0;
            label_HuongDan.Text = "Bấm vào ô để chọn Loại 1–4; chọn ô trống để xóa kết quả.";
            // 
            // kryptonButton_CapNhat
            // 
            kryptonButton_CapNhat.Anchor = AnchorStyles.Right;
            kryptonButton_CapNhat.Location = new Point(935, 7);
            kryptonButton_CapNhat.Margin = new Padding(0);
            kryptonButton_CapNhat.Name = "kryptonButton_CapNhat";
            kryptonButton_CapNhat.Size = new Size(154, 34);
            kryptonButton_CapNhat.StateCommon.Border.Rounding = 4F;
            kryptonButton_CapNhat.StateTracking.Border.Rounding = 4F;
            kryptonButton_CapNhat.TabIndex = 1;
            kryptonButton_CapNhat.Values.DropDownArrowColor = Color.Empty;
            kryptonButton_CapNhat.Values.Text = "Cập nhật";
            kryptonButton_CapNhat.Click += kryptonButton_CapNhat_Click;
            // 
            // Form62_ChinhSuaDataNhomTuan
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1121, 401);
            Controls.Add(tableLayoutPanel_Khung);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form62_ChinhSuaDataNhomTuan";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Kết quả thi đua theo tuần";
            tableLayoutPanel_Khung.ResumeLayout(false);
            tableLayoutPanel_Khung.PerformLayout();
            tableLayoutPanel_DauTrang.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            roundedPanel_Luoi.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView_LichXet).EndInit();
            tableLayoutPanel_Nut.ResumeLayout(false);
            tableLayoutPanel_Nut.PerformLayout();
            ResumeLayout(false);
        }
        #endregion
        private TableLayoutPanel tableLayoutPanel_Khung;
        private TableLayoutPanel tableLayoutPanel_DauTrang;
        private PictureBox pictureBox1;
        private NhanMotDong label_HoTen;
        private NhanMotDong label_SoHieu;
        private NhanMotDong label_DonVi;
        private RoundedPanel roundedPanel_Luoi;
        private DataGridView dataGridView_LichXet;
        private DataGridViewTextBoxColumn Cot_ThangXet;
        private DataGridViewComboBoxColumn Cot_Tuan1;
        private DataGridViewComboBoxColumn Cot_Tuan2;
        private DataGridViewComboBoxColumn Cot_Tuan3;
        private DataGridViewComboBoxColumn Cot_Tuan4;
        private DataGridViewComboBoxColumn Cot_KetQua;
        private Label label_ChuThich;
        private TableLayoutPanel tableLayoutPanel_Nut;
        private Label label_HuongDan;
        private Krypton.Toolkit.KryptonButton kryptonButton_CapNhat;
    }
}
