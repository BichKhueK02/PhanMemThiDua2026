using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PhanMemThiDua2026
{
    partial class Form38_XuatExcelTinhToan
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
            components = new Container();
            ComponentResourceManager resources = new ComponentResourceManager(typeof(Form38_XuatExcelTinhToan));
            tableLayoutPanel1 = new TableLayoutPanel();
            tableLayoutPanel2 = new TableLayoutPanel();
            tableLayoutPanel4 = new TableLayoutPanel();
            label_DuongDan = new Label();
            kryptonButton1_ChonDuongDan = new Krypton.Toolkit.KryptonButton();
            label2_ChonTep = new Label();
            tableLayoutPanel5 = new TableLayoutPanel();
            kryptonButton_TaoTepExcel = new Krypton.Toolkit.KryptonButton();
            groupBox1 = new GroupBox();
            tableLayoutPanel3 = new TableLayoutPanel();
            pictureBox3 = new PictureBox();
            radioButton1_XuatTheoPhanLoaiTangDan = new RadioButton();
            pictureBox2 = new PictureBox();
            radioButton1_XuatTheoThuTuTrongBienChe = new RadioButton();
            pictureBox1 = new PictureBox();
            toolTip1 = new ToolTip(components);
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            tableLayoutPanel4.SuspendLayout();
            tableLayoutPanel5.SuspendLayout();
            groupBox1.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            ((ISupportInitialize)pictureBox3).BeginInit();
            ((ISupportInitialize)pictureBox2).BeginInit();
            ((ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.BackColor = Color.White;
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.0041847F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 83.99582F));
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 1, 0);
            tableLayoutPanel1.Controls.Add(pictureBox1, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(3, 2, 3, 2);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(828, 274);
            tableLayoutPanel1.TabIndex = 1;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 1;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Controls.Add(tableLayoutPanel4, 0, 0);
            tableLayoutPanel2.Controls.Add(tableLayoutPanel5, 0, 2);
            tableLayoutPanel2.Controls.Add(groupBox1, 0, 1);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(135, 2);
            tableLayoutPanel2.Margin = new Padding(3, 2, 3, 2);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 3;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 28.7401581F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 47.6377945F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 24.015749F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 15F));
            tableLayoutPanel2.Size = new Size(690, 270);
            tableLayoutPanel2.TabIndex = 0;
            // 
            // tableLayoutPanel4
            // 
            tableLayoutPanel4.ColumnCount = 3;
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19.2161827F));
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5.941846F));
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 74.84197F));
            tableLayoutPanel4.Controls.Add(label_DuongDan, 2, 0);
            tableLayoutPanel4.Controls.Add(kryptonButton1_ChonDuongDan, 1, 0);
            tableLayoutPanel4.Controls.Add(label2_ChonTep, 0, 0);
            tableLayoutPanel4.Dock = DockStyle.Fill;
            tableLayoutPanel4.Location = new Point(3, 2);
            tableLayoutPanel4.Margin = new Padding(3, 2, 3, 2);
            tableLayoutPanel4.Name = "tableLayoutPanel4";
            tableLayoutPanel4.RowCount = 1;
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel4.Size = new Size(684, 73);
            tableLayoutPanel4.TabIndex = 10;
            // 
            // label_DuongDan
            // 
            label_DuongDan.Anchor = AnchorStyles.Left;
            label_DuongDan.AutoSize = true;
            label_DuongDan.Font = new Font("Segoe UI", 9.792F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label_DuongDan.ForeColor = Color.Blue;
            label_DuongDan.Location = new Point(174, 27);
            label_DuongDan.Name = "label_DuongDan";
            label_DuongDan.Size = new Size(100, 19);
            label_DuongDan.TabIndex = 11;
            label_DuongDan.Text = "Chưa chọn tệp";
            // 
            // kryptonButton1_ChonDuongDan
            // 
            kryptonButton1_ChonDuongDan.Anchor = AnchorStyles.Left;
            kryptonButton1_ChonDuongDan.Location = new Point(134, 24);
            kryptonButton1_ChonDuongDan.Margin = new Padding(3, 2, 3, 2);
            kryptonButton1_ChonDuongDan.Name = "kryptonButton1_ChonDuongDan";
            kryptonButton1_ChonDuongDan.Size = new Size(34, 25);
            kryptonButton1_ChonDuongDan.TabIndex = 0;
            kryptonButton1_ChonDuongDan.Values.DropDownArrowColor = Color.Empty;
            kryptonButton1_ChonDuongDan.Values.Text = "...";
            kryptonButton1_ChonDuongDan.Click += kryptonButton1_ChonDuongDan_Click;
            // 
            // label2_ChonTep
            // 
            label2_ChonTep.Anchor = AnchorStyles.Left;
            label2_ChonTep.AutoSize = true;
            label2_ChonTep.Font = new Font("Segoe UI", 10.0173912F, FontStyle.Italic, GraphicsUnit.Point, 0);
            label2_ChonTep.ForeColor = Color.FromArgb(0, 0, 192);
            label2_ChonTep.Location = new Point(3, 27);
            label2_ChonTep.Name = "label2_ChonTep";
            label2_ChonTep.Size = new Size(114, 19);
            label2_ChonTep.TabIndex = 2;
            label2_ChonTep.Text = "Chọn đường dẫn";
            // 
            // tableLayoutPanel5
            // 
            tableLayoutPanel5.ColumnCount = 1;
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 18F));
            tableLayoutPanel5.Controls.Add(kryptonButton_TaoTepExcel, 0, 0);
            tableLayoutPanel5.Dock = DockStyle.Fill;
            tableLayoutPanel5.Location = new Point(3, 207);
            tableLayoutPanel5.Margin = new Padding(3, 2, 3, 2);
            tableLayoutPanel5.Name = "tableLayoutPanel5";
            tableLayoutPanel5.RowCount = 1;
            tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel5.Size = new Size(684, 61);
            tableLayoutPanel5.TabIndex = 11;
            // 
            // kryptonButton_TaoTepExcel
            // 
            kryptonButton_TaoTepExcel.Anchor = AnchorStyles.None;
            kryptonButton_TaoTepExcel.Location = new Point(263, 18);
            kryptonButton_TaoTepExcel.Margin = new Padding(3, 2, 3, 2);
            kryptonButton_TaoTepExcel.Name = "kryptonButton_TaoTepExcel";
            kryptonButton_TaoTepExcel.Size = new Size(158, 25);
            kryptonButton_TaoTepExcel.StateCommon.Border.Rounding = 4F;
            kryptonButton_TaoTepExcel.TabIndex = 0;
            kryptonButton_TaoTepExcel.Values.DropDownArrowColor = Color.Empty;
            kryptonButton_TaoTepExcel.Values.Image = (Image)resources.GetObject("kryptonButton_TaoTepExcel.Values.Image");
            kryptonButton_TaoTepExcel.Values.Text = "Tạo tệp";
            kryptonButton_TaoTepExcel.Click += kryptonButton_TaoTepExcel_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(tableLayoutPanel3);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Font = new Font("Segoe UI", 8.765218F, FontStyle.Italic, GraphicsUnit.Point, 0);
            groupBox1.ForeColor = Color.FromArgb(0, 0, 192);
            groupBox1.Location = new Point(3, 79);
            groupBox1.Margin = new Padding(3, 2, 3, 2);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(3, 2, 3, 2);
            groupBox1.Size = new Size(684, 124);
            groupBox1.TabIndex = 12;
            groupBox1.TabStop = false;
            groupBox1.Text = "Chọn yêu cầu xuất tệp excel";
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.ColumnCount = 2;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9.566326F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 90.43367F));
            tableLayoutPanel3.Controls.Add(pictureBox3, 0, 1);
            tableLayoutPanel3.Controls.Add(radioButton1_XuatTheoPhanLoaiTangDan, 1, 1);
            tableLayoutPanel3.Controls.Add(pictureBox2, 0, 0);
            tableLayoutPanel3.Controls.Add(radioButton1_XuatTheoThuTuTrongBienChe, 1, 0);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new Point(3, 18);
            tableLayoutPanel3.Margin = new Padding(3, 2, 3, 2);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 2;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel3.Size = new Size(678, 104);
            tableLayoutPanel3.TabIndex = 0;
            // 
            // pictureBox3
            // 
            pictureBox3.Anchor = AnchorStyles.None;
            pictureBox3.Image = (Image)resources.GetObject("pictureBox3.Image");
            pictureBox3.Location = new Point(17, 67);
            pictureBox3.Margin = new Padding(3, 2, 3, 2);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new Size(29, 22);
            pictureBox3.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox3.TabIndex = 6;
            pictureBox3.TabStop = false;
            // 
            // radioButton1_XuatTheoPhanLoaiTangDan
            // 
            radioButton1_XuatTheoPhanLoaiTangDan.Anchor = AnchorStyles.Left;
            radioButton1_XuatTheoPhanLoaiTangDan.AutoSize = true;
            radioButton1_XuatTheoPhanLoaiTangDan.Location = new Point(67, 68);
            radioButton1_XuatTheoPhanLoaiTangDan.Margin = new Padding(3, 2, 3, 2);
            radioButton1_XuatTheoPhanLoaiTangDan.Name = "radioButton1_XuatTheoPhanLoaiTangDan";
            radioButton1_XuatTheoPhanLoaiTangDan.Size = new Size(264, 19);
            radioButton1_XuatTheoPhanLoaiTangDan.TabIndex = 5;
            radioButton1_XuatTheoPhanLoaiTangDan.TabStop = true;
            radioButton1_XuatTheoPhanLoaiTangDan.Text = "Xuất tệp Excel theo thứ tự phân loại tăng dần";
            radioButton1_XuatTheoPhanLoaiTangDan.UseVisualStyleBackColor = true;
            // 
            // pictureBox2
            // 
            pictureBox2.Anchor = AnchorStyles.None;
            pictureBox2.Image = (Image)resources.GetObject("pictureBox2.Image");
            pictureBox2.Location = new Point(17, 15);
            pictureBox2.Margin = new Padding(3, 2, 3, 2);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(29, 21);
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox2.TabIndex = 2;
            pictureBox2.TabStop = false;
            // 
            // radioButton1_XuatTheoThuTuTrongBienChe
            // 
            radioButton1_XuatTheoThuTuTrongBienChe.Anchor = AnchorStyles.Left;
            radioButton1_XuatTheoThuTuTrongBienChe.AutoSize = true;
            radioButton1_XuatTheoThuTuTrongBienChe.Location = new Point(67, 16);
            radioButton1_XuatTheoThuTuTrongBienChe.Margin = new Padding(3, 2, 3, 2);
            radioButton1_XuatTheoThuTuTrongBienChe.Name = "radioButton1_XuatTheoThuTuTrongBienChe";
            radioButton1_XuatTheoThuTuTrongBienChe.Size = new Size(206, 19);
            radioButton1_XuatTheoThuTuTrongBienChe.TabIndex = 4;
            radioButton1_XuatTheoThuTuTrongBienChe.TabStop = true;
            radioButton1_XuatTheoThuTuTrongBienChe.Text = "Xuất tệp Excel theo thứ tự biên chế";
            radioButton1_XuatTheoThuTuTrongBienChe.UseVisualStyleBackColor = true;
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.None;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(3, 87);
            pictureBox1.Margin = new Padding(3, 2, 3, 2);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(126, 100);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            // 
            // Form38_XuatExcelTinhToan
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(828, 274);
            Controls.Add(tableLayoutPanel1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 2, 3, 2);
            MaximizeBox = false;
            Name = "Form38_XuatExcelTinhToan";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Xuất tệp Excel tính toán";
            Load += Form38_XuatExcelTinhToan_Load;
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel4.ResumeLayout(false);
            tableLayoutPanel4.PerformLayout();
            tableLayoutPanel5.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            tableLayoutPanel3.ResumeLayout(false);
            tableLayoutPanel3.PerformLayout();
            ((ISupportInitialize)pictureBox3).EndInit();
            ((ISupportInitialize)pictureBox2).EndInit();
            ((ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel2;
        private TableLayoutPanel tableLayoutPanel4;
        private Label label_DuongDan;
        private Krypton.Toolkit.KryptonButton kryptonButton1_ChonDuongDan;
        private Label label2_ChonTep;
        private TableLayoutPanel tableLayoutPanel5;
        private Krypton.Toolkit.KryptonButton kryptonButton_TaoTepExcel;
        private GroupBox groupBox1;
        private PictureBox pictureBox1;
        private ToolTip toolTip1;
        private TableLayoutPanel tableLayoutPanel3;
        private RadioButton radioButton1_XuatTheoPhanLoaiTangDan;
        private PictureBox pictureBox2;
        private RadioButton radioButton1_XuatTheoThuTuTrongBienChe;
        private PictureBox pictureBox3;
    }
}