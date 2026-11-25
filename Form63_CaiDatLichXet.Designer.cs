namespace PhanMemThiDua2026
{
    partial class Form63_CaiDatLichXet
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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            tableLayoutPanel_Khung = new TableLayoutPanel();
            label_TieuDe = new Label();
            label_MoTa = new Label();
            label_ThangBatDau = new Label();
            comboBox_ThangBatDau = new ComboBox();
            label_SoThang = new Label();
            numericUpDown_SoThang = new NumericUpDown();
            checkBox_KetQuaThangSau = new CheckBox();
            label_XemTruoc = new Label();
            roundedPanel_XemTruoc = new RoundedPanel();
            dataGridView_XemTruoc = new DataGridView();
            Cot_Dot = new DataGridViewTextBoxColumn();
            Cot_TuanXet = new DataGridViewTextBoxColumn();
            Cot_GhiKetQua = new DataGridViewTextBoxColumn();
            label_CanhBao = new Label();
            flowLayoutPanel_Nut = new FlowLayoutPanel();
            kryptonButton_Luu = new Krypton.Toolkit.KryptonButton();
            kryptonButton_Huy = new Krypton.Toolkit.KryptonButton();
            kryptonButton_DatLaiMacDinh = new Krypton.Toolkit.KryptonButton();
            tableLayoutPanel_Khung.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_SoThang).BeginInit();
            roundedPanel_XemTruoc.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_XemTruoc).BeginInit();
            flowLayoutPanel_Nut.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel_Khung
            // 
            tableLayoutPanel_Khung.BackColor = Color.White;
            tableLayoutPanel_Khung.ColumnCount = 4;
            tableLayoutPanel_Khung.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel_Khung.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel_Khung.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel_Khung.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel_Khung.Controls.Add(label_TieuDe, 0, 0);
            tableLayoutPanel_Khung.Controls.Add(label_MoTa, 0, 1);
            tableLayoutPanel_Khung.Controls.Add(label_ThangBatDau, 0, 2);
            tableLayoutPanel_Khung.Controls.Add(comboBox_ThangBatDau, 1, 2);
            tableLayoutPanel_Khung.Controls.Add(label_SoThang, 2, 2);
            tableLayoutPanel_Khung.Controls.Add(numericUpDown_SoThang, 3, 2);
            tableLayoutPanel_Khung.Controls.Add(checkBox_KetQuaThangSau, 0, 3);
            tableLayoutPanel_Khung.Controls.Add(label_XemTruoc, 0, 4);
            tableLayoutPanel_Khung.Controls.Add(roundedPanel_XemTruoc, 0, 5);
            tableLayoutPanel_Khung.Controls.Add(label_CanhBao, 0, 6);
            tableLayoutPanel_Khung.Controls.Add(flowLayoutPanel_Nut, 0, 7);
            tableLayoutPanel_Khung.Dock = DockStyle.Fill;
            tableLayoutPanel_Khung.Location = new Point(0, 0);
            tableLayoutPanel_Khung.Name = "tableLayoutPanel_Khung";
            tableLayoutPanel_Khung.Padding = new Padding(18, 14, 18, 10);
            tableLayoutPanel_Khung.RowCount = 8;
            tableLayoutPanel_Khung.RowStyles.Add(new RowStyle());
            tableLayoutPanel_Khung.RowStyles.Add(new RowStyle());
            tableLayoutPanel_Khung.RowStyles.Add(new RowStyle());
            tableLayoutPanel_Khung.RowStyles.Add(new RowStyle());
            tableLayoutPanel_Khung.RowStyles.Add(new RowStyle());
            tableLayoutPanel_Khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel_Khung.RowStyles.Add(new RowStyle());
            tableLayoutPanel_Khung.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            tableLayoutPanel_Khung.Size = new Size(806, 511);
            tableLayoutPanel_Khung.TabIndex = 0;
            // 
            // label_TieuDe
            // 
            label_TieuDe.AutoSize = true;
            tableLayoutPanel_Khung.SetColumnSpan(label_TieuDe, 4);
            label_TieuDe.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            label_TieuDe.ForeColor = Color.FromArgb(91, 33, 182);
            label_TieuDe.Location = new Point(18, 14);
            label_TieuDe.Margin = new Padding(0, 0, 0, 2);
            label_TieuDe.Name = "label_TieuDe";
            label_TieuDe.Size = new Size(147, 25);
            label_TieuDe.TabIndex = 0;
            label_TieuDe.Text = "Lịch xét thi đua";
            // 
            // label_MoTa
            // 
            label_MoTa.AutoSize = true;
            tableLayoutPanel_Khung.SetColumnSpan(label_MoTa, 4);
            label_MoTa.Font = new Font("Segoe UI", 9.75F);
            label_MoTa.ForeColor = Color.DimGray;
            label_MoTa.Location = new Point(18, 41);
            label_MoTa.Margin = new Padding(0, 0, 0, 12);
            label_MoTa.MaximumSize = new Size(628, 0);
            label_MoTa.Name = "label_MoTa";
            label_MoTa.Size = new Size(608, 34);
            label_MoTa.TabIndex = 1;
            label_MoTa.Text = "Xét theo tuần: mỗi tháng 4 tuần, lấy kết quả 4 tuần để xét tháng. Chọn tháng bắt đầu và số tháng theo từng khóa; khóa được kéo sang năm sau.";
            // 
            // label_ThangBatDau
            // 
            label_ThangBatDau.Anchor = AnchorStyles.Left;
            label_ThangBatDau.AutoSize = true;
            label_ThangBatDau.Font = new Font("Segoe UI", 10F);
            label_ThangBatDau.Location = new Point(18, 94);
            label_ThangBatDau.Margin = new Padding(0, 0, 10, 0);
            label_ThangBatDau.Name = "label_ThangBatDau";
            label_ThangBatDau.Size = new Size(101, 19);
            label_ThangBatDau.TabIndex = 2;
            label_ThangBatDau.Text = "Tháng bắt đầu:";
            // 
            // comboBox_ThangBatDau
            // 
            comboBox_ThangBatDau.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            comboBox_ThangBatDau.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_ThangBatDau.Font = new Font("Segoe UI", 10F);
            comboBox_ThangBatDau.FormattingEnabled = true;
            comboBox_ThangBatDau.Items.AddRange(new object[] { "Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6", "Tháng 7", "Tháng 8", "Tháng 9", "Tháng 10", "Tháng 11", "Tháng 12" });
            comboBox_ThangBatDau.Location = new Point(129, 87);
            comboBox_ThangBatDau.Margin = new Padding(0, 0, 24, 6);
            comboBox_ThangBatDau.Name = "comboBox_ThangBatDau";
            comboBox_ThangBatDau.Size = new Size(249, 25);
            comboBox_ThangBatDau.TabIndex = 3;
            // 
            // label_SoThang
            // 
            label_SoThang.Anchor = AnchorStyles.Left;
            label_SoThang.AutoSize = true;
            label_SoThang.Font = new Font("Segoe UI", 10F);
            label_SoThang.Location = new Point(402, 94);
            label_SoThang.Margin = new Padding(0, 0, 10, 0);
            label_SoThang.Name = "label_SoThang";
            label_SoThang.Size = new Size(102, 19);
            label_SoThang.TabIndex = 4;
            label_SoThang.Text = "Số tháng (1–6):";
            // 
            // numericUpDown_SoThang
            // 
            numericUpDown_SoThang.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            numericUpDown_SoThang.Font = new Font("Segoe UI", 10F);
            numericUpDown_SoThang.Location = new Point(514, 88);
            numericUpDown_SoThang.Margin = new Padding(0, 0, 0, 6);
            numericUpDown_SoThang.Maximum = new decimal(new int[] { 6, 0, 0, 0 });
            numericUpDown_SoThang.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDown_SoThang.Name = "numericUpDown_SoThang";
            numericUpDown_SoThang.Size = new Size(274, 25);
            numericUpDown_SoThang.TabIndex = 5;
            numericUpDown_SoThang.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // checkBox_KetQuaThangSau
            // 
            checkBox_KetQuaThangSau.AutoSize = true;
            tableLayoutPanel_Khung.SetColumnSpan(checkBox_KetQuaThangSau, 4);
            checkBox_KetQuaThangSau.Font = new Font("Segoe UI", 10F);
            checkBox_KetQuaThangSau.Location = new Point(18, 123);
            checkBox_KetQuaThangSau.Margin = new Padding(0, 3, 0, 10);
            checkBox_KetQuaThangSau.Name = "checkBox_KetQuaThangSau";
            checkBox_KetQuaThangSau.Size = new Size(453, 23);
            checkBox_KetQuaThangSau.TabIndex = 6;
            checkBox_KetQuaThangSau.Text = "Kết quả ghi cho tháng kế tiếp (vd: 4 tuần tháng 2 → kết quả tháng 3)";
            checkBox_KetQuaThangSau.UseVisualStyleBackColor = true;
            // 
            // label_XemTruoc
            // 
            label_XemTruoc.AutoSize = true;
            tableLayoutPanel_Khung.SetColumnSpan(label_XemTruoc, 4);
            label_XemTruoc.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label_XemTruoc.ForeColor = Color.FromArgb(55, 65, 81);
            label_XemTruoc.Location = new Point(18, 156);
            label_XemTruoc.Margin = new Padding(0, 0, 0, 6);
            label_XemTruoc.Name = "label_XemTruoc";
            label_XemTruoc.Size = new Size(135, 19);
            label_XemTruoc.TabIndex = 7;
            label_XemTruoc.Text = "Xem trước lịch xét:";
            // 
            // roundedPanel_XemTruoc
            // 
            roundedPanel_XemTruoc.BackColor = Color.White;
            roundedPanel_XemTruoc.BoGocControlCon = true;
            roundedPanel_XemTruoc.BorderColor = Color.FromArgb(221, 214, 254);
            roundedPanel_XemTruoc.BorderWidth = 1;
            tableLayoutPanel_Khung.SetColumnSpan(roundedPanel_XemTruoc, 4);
            roundedPanel_XemTruoc.Controls.Add(dataGridView_XemTruoc);
            roundedPanel_XemTruoc.Dock = DockStyle.Fill;
            roundedPanel_XemTruoc.FillColor = Color.White;
            roundedPanel_XemTruoc.Location = new Point(18, 181);
            roundedPanel_XemTruoc.Margin = new Padding(0, 0, 0, 6);
            roundedPanel_XemTruoc.Name = "roundedPanel_XemTruoc";
            roundedPanel_XemTruoc.Padding = new Padding(1);
            roundedPanel_XemTruoc.Radius = 10;
            roundedPanel_XemTruoc.Size = new Size(770, 249);
            roundedPanel_XemTruoc.TabIndex = 8;
            // 
            // dataGridView_XemTruoc
            // 
            dataGridView_XemTruoc.AllowUserToAddRows = false;
            dataGridView_XemTruoc.AllowUserToDeleteRows = false;
            dataGridView_XemTruoc.AllowUserToResizeColumns = false;
            dataGridView_XemTruoc.AllowUserToResizeRows = false;
            dataGridView_XemTruoc.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView_XemTruoc.BackgroundColor = Color.White;
            dataGridView_XemTruoc.BorderStyle = BorderStyle.None;
            dataGridView_XemTruoc.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(237, 233, 254);
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = Color.FromArgb(91, 33, 182);
            dataGridViewCellStyle1.SelectionBackColor = Color.FromArgb(237, 233, 254);
            dataGridViewCellStyle1.SelectionForeColor = Color.FromArgb(91, 33, 182);
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dataGridView_XemTruoc.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dataGridView_XemTruoc.ColumnHeadersHeight = 38;
            dataGridView_XemTruoc.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridView_XemTruoc.Columns.AddRange(new DataGridViewColumn[] { Cot_Dot, Cot_TuanXet, Cot_GhiKetQua });
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = Color.White;
            dataGridViewCellStyle3.Font = new Font("Segoe UI", 10F);
            dataGridViewCellStyle3.ForeColor = Color.Black;
            dataGridViewCellStyle3.SelectionBackColor = Color.White;
            dataGridViewCellStyle3.SelectionForeColor = Color.Black;
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
            dataGridView_XemTruoc.DefaultCellStyle = dataGridViewCellStyle3;
            dataGridView_XemTruoc.Dock = DockStyle.Fill;
            dataGridView_XemTruoc.EnableHeadersVisualStyles = false;
            dataGridView_XemTruoc.GridColor = Color.FromArgb(221, 214, 254);
            dataGridView_XemTruoc.Location = new Point(1, 1);
            dataGridView_XemTruoc.Margin = new Padding(0);
            dataGridView_XemTruoc.MultiSelect = false;
            dataGridView_XemTruoc.Name = "dataGridView_XemTruoc";
            dataGridView_XemTruoc.ReadOnly = true;
            dataGridView_XemTruoc.RowHeadersVisible = false;
            dataGridView_XemTruoc.RowTemplate.Height = 30;
            dataGridView_XemTruoc.ScrollBars = ScrollBars.None;
            dataGridView_XemTruoc.Size = new Size(768, 247);
            dataGridView_XemTruoc.TabIndex = 0;
            dataGridView_XemTruoc.TabStop = false;
            // 
            // Cot_Dot
            // 
            dataGridViewCellStyle2.BackColor = Color.FromArgb(250, 249, 255);
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dataGridViewCellStyle2.ForeColor = Color.FromArgb(55, 65, 81);
            dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(250, 249, 255);
            dataGridViewCellStyle2.SelectionForeColor = Color.FromArgb(55, 65, 81);
            Cot_Dot.DefaultCellStyle = dataGridViewCellStyle2;
            Cot_Dot.FillWeight = 50F;
            Cot_Dot.HeaderText = "Đợt";
            Cot_Dot.Name = "Cot_Dot";
            Cot_Dot.ReadOnly = true;
            Cot_Dot.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // Cot_TuanXet
            // 
            Cot_TuanXet.HeaderText = "Xét 4 tuần của";
            Cot_TuanXet.Name = "Cot_TuanXet";
            Cot_TuanXet.ReadOnly = true;
            Cot_TuanXet.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // Cot_GhiKetQua
            // 
            Cot_GhiKetQua.HeaderText = "Ghi kết quả tháng";
            Cot_GhiKetQua.Name = "Cot_GhiKetQua";
            Cot_GhiKetQua.ReadOnly = true;
            Cot_GhiKetQua.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // label_CanhBao
            // 
            label_CanhBao.AutoSize = true;
            tableLayoutPanel_Khung.SetColumnSpan(label_CanhBao, 4);
            label_CanhBao.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            label_CanhBao.ForeColor = Color.Firebrick;
            label_CanhBao.Location = new Point(18, 436);
            label_CanhBao.Margin = new Padding(0, 0, 0, 4);
            label_CanhBao.MaximumSize = new Size(628, 0);
            label_CanhBao.Name = "label_CanhBao";
            label_CanhBao.Size = new Size(0, 15);
            label_CanhBao.TabIndex = 9;
            // 
            // flowLayoutPanel_Nut
            // 
            tableLayoutPanel_Khung.SetColumnSpan(flowLayoutPanel_Nut, 4);
            flowLayoutPanel_Nut.Controls.Add(kryptonButton_Luu);
            flowLayoutPanel_Nut.Controls.Add(kryptonButton_Huy);
            flowLayoutPanel_Nut.Controls.Add(kryptonButton_DatLaiMacDinh);
            flowLayoutPanel_Nut.Dock = DockStyle.Fill;
            flowLayoutPanel_Nut.FlowDirection = FlowDirection.RightToLeft;
            flowLayoutPanel_Nut.Location = new Point(18, 455);
            flowLayoutPanel_Nut.Margin = new Padding(0);
            flowLayoutPanel_Nut.Name = "flowLayoutPanel_Nut";
            flowLayoutPanel_Nut.Padding = new Padding(0, 6, 0, 0);
            flowLayoutPanel_Nut.Size = new Size(770, 46);
            flowLayoutPanel_Nut.TabIndex = 10;
            // 
            // kryptonButton_Luu
            // 
            kryptonButton_Luu.Location = new Point(650, 6);
            kryptonButton_Luu.Margin = new Padding(10, 0, 0, 0);
            kryptonButton_Luu.Name = "kryptonButton_Luu";
            kryptonButton_Luu.Size = new Size(120, 34);
            kryptonButton_Luu.StateCommon.Border.Rounding = 4F;
            kryptonButton_Luu.StateTracking.Border.Rounding = 4F;
            kryptonButton_Luu.TabIndex = 0;
            kryptonButton_Luu.Values.DropDownArrowColor = Color.Empty;
            kryptonButton_Luu.Values.Text = "Lưu";
            kryptonButton_Luu.Click += kryptonButton_Luu_Click;
            // 
            // kryptonButton_Huy
            // 
            kryptonButton_Huy.DialogResult = DialogResult.Cancel;
            kryptonButton_Huy.Location = new Point(520, 6);
            kryptonButton_Huy.Margin = new Padding(10, 0, 0, 0);
            kryptonButton_Huy.Name = "kryptonButton_Huy";
            kryptonButton_Huy.Size = new Size(120, 34);
            kryptonButton_Huy.StateCommon.Border.Rounding = 4F;
            kryptonButton_Huy.StateTracking.Border.Rounding = 4F;
            kryptonButton_Huy.TabIndex = 1;
            kryptonButton_Huy.Values.DropDownArrowColor = Color.Empty;
            kryptonButton_Huy.Values.Text = "Đóng";
            // 
            // kryptonButton_DatLaiMacDinh
            // 
            kryptonButton_DatLaiMacDinh.Location = new Point(340, 6);
            kryptonButton_DatLaiMacDinh.Margin = new Padding(10, 0, 0, 0);
            kryptonButton_DatLaiMacDinh.Name = "kryptonButton_DatLaiMacDinh";
            kryptonButton_DatLaiMacDinh.Size = new Size(170, 34);
            kryptonButton_DatLaiMacDinh.StateCommon.Border.Rounding = 4F;
            kryptonButton_DatLaiMacDinh.StateTracking.Border.Rounding = 4F;
            kryptonButton_DatLaiMacDinh.TabIndex = 2;
            kryptonButton_DatLaiMacDinh.Values.DropDownArrowColor = Color.Empty;
            kryptonButton_DatLaiMacDinh.Values.Text = "Đặt lại mặc định";
            kryptonButton_DatLaiMacDinh.Click += kryptonButton_DatLaiMacDinh_Click;
            // 
            // Form63_CaiDatLichXet
            // 
            AcceptButton = kryptonButton_Luu;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            CancelButton = kryptonButton_Huy;
            ClientSize = new Size(806, 511);
            Controls.Add(tableLayoutPanel_Khung);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form63_CaiDatLichXet";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Lịch xét thi đua";
            tableLayoutPanel_Khung.ResumeLayout(false);
            tableLayoutPanel_Khung.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_SoThang).EndInit();
            roundedPanel_XemTruoc.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView_XemTruoc).EndInit();
            flowLayoutPanel_Nut.ResumeLayout(false);
            ResumeLayout(false);
        }
        #endregion
        private TableLayoutPanel tableLayoutPanel_Khung;
        private Label label_TieuDe;
        private Label label_MoTa;
        private Label label_ThangBatDau;
        private ComboBox comboBox_ThangBatDau;
        private Label label_SoThang;
        private NumericUpDown numericUpDown_SoThang;
        private CheckBox checkBox_KetQuaThangSau;
        private Label label_XemTruoc;
        private RoundedPanel roundedPanel_XemTruoc;
        private DataGridView dataGridView_XemTruoc;
        private DataGridViewTextBoxColumn Cot_Dot;
        private DataGridViewTextBoxColumn Cot_TuanXet;
        private DataGridViewTextBoxColumn Cot_GhiKetQua;
        private Label label_CanhBao;
        private FlowLayoutPanel flowLayoutPanel_Nut;
        private Krypton.Toolkit.KryptonButton kryptonButton_Luu;
        private Krypton.Toolkit.KryptonButton kryptonButton_Huy;
        private Krypton.Toolkit.KryptonButton kryptonButton_DatLaiMacDinh;
    }
}
