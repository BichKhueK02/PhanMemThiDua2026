using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Krypton.Toolkit;

namespace PhanMemThiDua2026
{
    partial class Form24_XacMinhAdmin
    {
        private IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new Container();
            ComponentResourceManager resources = new ComponentResourceManager(typeof(Form24_XacMinhAdmin));
            PictureBox1 = new PictureBox();
            label1 = new Label();
            label2 = new Label();
            label_TieuDe = new Label();
            label_MoTa = new Label();
            text_TenDangNhap = new KryptonTextBox();
            text_MatKhau = new KryptonTextBox();
            check_HienMatKhau = new CheckBox();
            btn_XacThuc = new KryptonButton();
            btn_Thoat = new KryptonButton();
            panel_Card = new KryptonGroup();
            TableLayoutPanel5 = new TableLayoutPanel();
            TableLayoutPanel1 = new TableLayoutPanel();
            TableLayoutPanel2 = new TableLayoutPanel();
            TableLayoutPanel3 = new TableLayoutPanel();
            toolTip1 = new ToolTip(components);
            ((ISupportInitialize)PictureBox1).BeginInit();
            ((ISupportInitialize)panel_Card).BeginInit();
            ((ISupportInitialize)panel_Card.Panel).BeginInit();
            panel_Card.Panel.SuspendLayout();
            TableLayoutPanel5.SuspendLayout();
            TableLayoutPanel1.SuspendLayout();
            TableLayoutPanel2.SuspendLayout();
            TableLayoutPanel3.SuspendLayout();
            SuspendLayout();
            // 
            // PictureBox1
            // 
            PictureBox1.Anchor = AnchorStyles.None;
            PictureBox1.BackColor = Color.Transparent;
            PictureBox1.Image = (Image)resources.GetObject("PictureBox1.Image");
            PictureBox1.Location = new Point(149, 32);
            PictureBox1.Margin = new Padding(0);
            PictureBox1.Name = "PictureBox1";
            PictureBox1.Size = new Size(90, 83);
            PictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            PictureBox1.TabIndex = 0;
            PictureBox1.TabStop = false;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Left;
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            label1.ForeColor = Color.FromArgb(51, 65, 85);
            label1.Location = new Point(0, 5);
            label1.Margin = new Padding(0);
            label1.Name = "label1";
            label1.Size = new Size(99, 17);
            label1.TabIndex = 3;
            label1.Text = "Tên đăng nhập";
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Left;
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            label2.ForeColor = Color.FromArgb(51, 65, 85);
            label2.Location = new Point(0, 5);
            label2.Margin = new Padding(0);
            label2.Name = "label2";
            label2.Size = new Size(66, 17);
            label2.TabIndex = 5;
            label2.Text = "Mật khẩu";
            // 
            // label_TieuDe
            // 
            label_TieuDe.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            label_TieuDe.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            label_TieuDe.ForeColor = Color.FromArgb(15, 23, 42);
            label_TieuDe.Location = new Point(24, 115);
            label_TieuDe.Margin = new Padding(0);
            label_TieuDe.Name = "label_TieuDe";
            label_TieuDe.Size = new Size(340, 38);
            label_TieuDe.TabIndex = 1;
            label_TieuDe.Text = "Xác minh quản trị viên";
            label_TieuDe.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label_MoTa
            // 
            label_MoTa.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            label_MoTa.Font = new Font("Segoe UI", 10F);
            label_MoTa.ForeColor = Color.FromArgb(100, 116, 139);
            label_MoTa.Location = new Point(24, 154);
            label_MoTa.Margin = new Padding(0);
            label_MoTa.Name = "label_MoTa";
            label_MoTa.Size = new Size(340, 25);
            label_MoTa.TabIndex = 2;
            label_MoTa.Text = "Vui lòng nhập tài khoản quản trị để tiếp tục.";
            label_MoTa.TextAlign = ContentAlignment.TopCenter;
            // 
            // text_TenDangNhap
            // 
            text_TenDangNhap.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            text_TenDangNhap.Location = new Point(0, 32);
            text_TenDangNhap.Margin = new Padding(0);
            text_TenDangNhap.Name = "text_TenDangNhap";
            text_TenDangNhap.Size = new Size(340, 39);
            text_TenDangNhap.StateCommon.Back.Color1 = Color.White;
            text_TenDangNhap.StateCommon.Border.Color1 = Color.FromArgb(203, 213, 225);
            text_TenDangNhap.StateCommon.Border.Color2 = Color.FromArgb(203, 213, 225);
            text_TenDangNhap.StateCommon.Border.DrawBorders = PaletteDrawBorders.Top | PaletteDrawBorders.Bottom | PaletteDrawBorders.Left | PaletteDrawBorders.Right;
            text_TenDangNhap.StateCommon.Border.Rounding = 8F;
            text_TenDangNhap.StateCommon.Border.Width = 1;
            text_TenDangNhap.StateCommon.Content.Color1 = Color.FromArgb(15, 23, 42);
            text_TenDangNhap.StateCommon.Content.Font = new Font("Segoe UI", 10.5F);
            text_TenDangNhap.StateCommon.Content.Padding = new Padding(12, 6, 12, 6);
            text_TenDangNhap.TabIndex = 1;
            // 
            // text_MatKhau
            // 
            text_MatKhau.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            text_MatKhau.Location = new Point(0, 32);
            text_MatKhau.Margin = new Padding(0);
            text_MatKhau.Name = "text_MatKhau";
            text_MatKhau.PasswordChar = '●';
            text_MatKhau.Size = new Size(340, 39);
            text_MatKhau.StateCommon.Back.Color1 = Color.White;
            text_MatKhau.StateCommon.Border.Color1 = Color.FromArgb(203, 213, 225);
            text_MatKhau.StateCommon.Border.Color2 = Color.FromArgb(203, 213, 225);
            text_MatKhau.StateCommon.Border.DrawBorders = PaletteDrawBorders.Top | PaletteDrawBorders.Bottom | PaletteDrawBorders.Left | PaletteDrawBorders.Right;
            text_MatKhau.StateCommon.Border.Rounding = 8F;
            text_MatKhau.StateCommon.Border.Width = 1;
            text_MatKhau.StateCommon.Content.Color1 = Color.FromArgb(15, 23, 42);
            text_MatKhau.StateCommon.Content.Font = new Font("Segoe UI", 10.5F);
            text_MatKhau.StateCommon.Content.Padding = new Padding(12, 6, 12, 6);
            text_MatKhau.TabIndex = 2;
            // 
            // check_HienMatKhau
            // 
            check_HienMatKhau.Anchor = AnchorStyles.Left;
            check_HienMatKhau.AutoSize = true;
            check_HienMatKhau.Cursor = Cursors.Hand;
            check_HienMatKhau.Font = new Font("Segoe UI", 9.5F);
            check_HienMatKhau.ForeColor = Color.FromArgb(71, 85, 105);
            check_HienMatKhau.Location = new Point(4, 158);
            check_HienMatKhau.Margin = new Padding(4, 0, 0, 0);
            check_HienMatKhau.Name = "check_HienMatKhau";
            check_HienMatKhau.Size = new Size(110, 21);
            check_HienMatKhau.TabIndex = 3;
            check_HienMatKhau.Text = "Hiện mật khẩu";
            check_HienMatKhau.UseVisualStyleBackColor = true;
            check_HienMatKhau.CheckedChanged += check_HienMatKhau_CheckedChanged;
            // 
            // btn_XacThuc
            // 
            btn_XacThuc.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            btn_XacThuc.Cursor = Cursors.Hand;
            btn_XacThuc.Location = new Point(0, 191);
            btn_XacThuc.Margin = new Padding(0);
            btn_XacThuc.Name = "btn_XacThuc";
            btn_XacThuc.Size = new Size(340, 44);
            btn_XacThuc.StateCommon.Back.Color1 = Color.FromArgb(99, 102, 241);
            btn_XacThuc.StateCommon.Back.Color2 = Color.FromArgb(99, 102, 241);
            btn_XacThuc.StateCommon.Border.DrawBorders = PaletteDrawBorders.Top | PaletteDrawBorders.Bottom | PaletteDrawBorders.Left | PaletteDrawBorders.Right;
            btn_XacThuc.StateCommon.Border.Rounding = 8F;
            btn_XacThuc.StateCommon.Border.Width = 0;
            btn_XacThuc.StateCommon.Content.ShortText.Color1 = Color.White;
            btn_XacThuc.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            btn_XacThuc.StatePressed.Back.Color1 = Color.FromArgb(67, 56, 202);
            btn_XacThuc.StatePressed.Back.Color2 = Color.FromArgb(67, 56, 202);
            btn_XacThuc.StateTracking.Back.Color1 = Color.FromArgb(79, 70, 229);
            btn_XacThuc.StateTracking.Back.Color2 = Color.FromArgb(79, 70, 229);
            btn_XacThuc.TabIndex = 4;
            btn_XacThuc.Values.DropDownArrowColor = Color.Empty;
            btn_XacThuc.Values.Image = (Image)resources.GetObject("btn_XacThuc.Values.Image");
            btn_XacThuc.Values.Text = " Xác thực";
            btn_XacThuc.Click += btn_XacThuc_Click;
            // 
            // btn_Thoat
            // 
            btn_Thoat.Anchor = AnchorStyles.None;
            btn_Thoat.Cursor = Cursors.Hand;
            btn_Thoat.Location = new Point(29, 428);
            btn_Thoat.Margin = new Padding(0);
            btn_Thoat.Name = "btn_Thoat";
            btn_Thoat.Size = new Size(329, 42);
            btn_Thoat.StateCommon.Back.Color1 = Color.White;
            btn_Thoat.StateCommon.Back.Color2 = Color.White;
            btn_Thoat.StateCommon.Border.Color1 = Color.FromArgb(226, 232, 240);
            btn_Thoat.StateCommon.Border.Color2 = Color.FromArgb(226, 232, 240);
            btn_Thoat.StateCommon.Border.DrawBorders = PaletteDrawBorders.Top | PaletteDrawBorders.Bottom | PaletteDrawBorders.Left | PaletteDrawBorders.Right;
            btn_Thoat.StateCommon.Border.Rounding = 8F;
            btn_Thoat.StateCommon.Border.Width = 1;
            btn_Thoat.StateCommon.Content.ShortText.Color1 = Color.FromArgb(71, 85, 105);
            btn_Thoat.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            btn_Thoat.StatePressed.Back.Color1 = Color.FromArgb(241, 245, 249);
            btn_Thoat.StatePressed.Back.Color2 = Color.FromArgb(241, 245, 249);
            btn_Thoat.StateTracking.Back.Color1 = Color.FromArgb(248, 250, 252);
            btn_Thoat.StateTracking.Back.Color2 = Color.FromArgb(248, 250, 252);
            btn_Thoat.TabIndex = 5;
            btn_Thoat.Values.DropDownArrowColor = Color.Empty;
            btn_Thoat.Values.Image = (Image)resources.GetObject("btn_Thoat.Values.Image");
            btn_Thoat.Values.Text = " Thoát";
            btn_Thoat.Click += btn_Thoat_Click;
            // 
            // panel_Card
            // 
            // 
            // panel_Card
            // 
            panel_Card.Anchor = AnchorStyles.None; // Thêm dòng này để giữ thẻ ở giữa khi Form co giãn
            panel_Card.Location = new Point(46, 18); // Chỉnh lại tọa độ căn giữa chuẩn với ClientSize (493, 551)
            panel_Card.Panel.Controls.Add(TableLayoutPanel5);
            panel_Card.Size = new Size(400, 514);
            panel_Card.StateCommon.Back.Color1 = Color.White;
            panel_Card.StateCommon.Back.Color2 = Color.White;
            panel_Card.StateCommon.Border.Color1 = Color.FromArgb(226, 232, 240);
            panel_Card.StateCommon.Border.Color2 = Color.FromArgb(226, 232, 240);
            panel_Card.StateCommon.Border.DrawBorders = PaletteDrawBorders.Top | PaletteDrawBorders.Bottom | PaletteDrawBorders.Left | PaletteDrawBorders.Right;
            panel_Card.StateCommon.Border.Rounding = 16F;
            panel_Card.StateCommon.Border.Width = 1;
            panel_Card.TabIndex = 9;
            // 
            // TableLayoutPanel5
            // 
            TableLayoutPanel5.BackColor = Color.Transparent;
            TableLayoutPanel5.ColumnCount = 1;
            TableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            TableLayoutPanel5.Controls.Add(PictureBox1, 0, 0);
            TableLayoutPanel5.Controls.Add(label_TieuDe, 0, 1);
            TableLayoutPanel5.Controls.Add(label_MoTa, 0, 2);
            TableLayoutPanel5.Controls.Add(TableLayoutPanel1, 0, 3);
            TableLayoutPanel5.Controls.Add(btn_Thoat, 0, 4);
            TableLayoutPanel5.Dock = DockStyle.Fill;
            TableLayoutPanel5.Location = new Point(0, 0);
            TableLayoutPanel5.Margin = new Padding(0);
            TableLayoutPanel5.Name = "TableLayoutPanel5";
            TableLayoutPanel5.Padding = new Padding(24, 32, 24, 24);
            TableLayoutPanel5.RowCount = 5;
            TableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Absolute, 83F));
            TableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            TableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            TableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Absolute, 240F));
            TableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            TableLayoutPanel5.Size = new Size(388, 502);
            TableLayoutPanel5.TabIndex = 10;
            // 
            // TableLayoutPanel1
            // 
            TableLayoutPanel1.ColumnCount = 1;
            TableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            TableLayoutPanel1.Controls.Add(TableLayoutPanel2, 0, 0);
            TableLayoutPanel1.Controls.Add(TableLayoutPanel3, 0, 1);
            TableLayoutPanel1.Controls.Add(check_HienMatKhau, 0, 2);
            TableLayoutPanel1.Controls.Add(btn_XacThuc, 0, 3);
            TableLayoutPanel1.Dock = DockStyle.Fill;
            TableLayoutPanel1.Location = new Point(24, 181);
            TableLayoutPanel1.Margin = new Padding(0);
            TableLayoutPanel1.Name = "TableLayoutPanel1";
            TableLayoutPanel1.RowCount = 4;
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            TableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            TableLayoutPanel1.Size = new Size(340, 240);
            TableLayoutPanel1.TabIndex = 8;
            // 
            // TableLayoutPanel2
            // 
            TableLayoutPanel2.ColumnCount = 1;
            TableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            TableLayoutPanel2.Controls.Add(label1, 0, 0);
            TableLayoutPanel2.Controls.Add(text_TenDangNhap, 0, 1);
            TableLayoutPanel2.Dock = DockStyle.Fill;
            TableLayoutPanel2.Location = new Point(0, 0);
            TableLayoutPanel2.Margin = new Padding(0);
            TableLayoutPanel2.Name = "TableLayoutPanel2";
            TableLayoutPanel2.RowCount = 2;
            TableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            TableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            TableLayoutPanel2.Size = new Size(340, 76);
            TableLayoutPanel2.TabIndex = 6;
            // 
            // TableLayoutPanel3
            // 
            TableLayoutPanel3.ColumnCount = 1;
            TableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            TableLayoutPanel3.Controls.Add(label2, 0, 0);
            TableLayoutPanel3.Controls.Add(text_MatKhau, 0, 1);
            TableLayoutPanel3.Dock = DockStyle.Fill;
            TableLayoutPanel3.Location = new Point(0, 76);
            TableLayoutPanel3.Margin = new Padding(0);
            TableLayoutPanel3.Name = "TableLayoutPanel3";
            TableLayoutPanel3.RowCount = 2;
            TableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            TableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            TableLayoutPanel3.Size = new Size(340, 76);
            TableLayoutPanel3.TabIndex = 7;
            // 
            // Form24_XacMinhAdmin
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(248, 250, 252);
            ClientSize = new Size(493, 551);
            Controls.Add(panel_Card);
            Font = new Font("Segoe UI", 9.5F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form24_XacMinhAdmin";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Xác minh quản trị viên";
            ((ISupportInitialize)PictureBox1).EndInit();
            ((ISupportInitialize)panel_Card.Panel).EndInit();
            panel_Card.Panel.ResumeLayout(false);
            ((ISupportInitialize)panel_Card).EndInit();
            TableLayoutPanel5.ResumeLayout(false);
            TableLayoutPanel1.ResumeLayout(false);
            TableLayoutPanel1.PerformLayout();
            TableLayoutPanel2.ResumeLayout(false);
            TableLayoutPanel2.PerformLayout();
            TableLayoutPanel3.ResumeLayout(false);
            TableLayoutPanel3.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        internal PictureBox PictureBox1;
        internal CheckBox check_HienMatKhau;

        private KryptonTextBox text_MatKhau;
        private KryptonTextBox text_TenDangNhap;

        internal Label label1;
        internal Label label2;
        private Label label_TieuDe;
        private Label label_MoTa;

        private KryptonButton btn_XacThuc;
        private KryptonButton btn_Thoat;

        internal TableLayoutPanel TableLayoutPanel1;
        internal TableLayoutPanel TableLayoutPanel2;
        internal TableLayoutPanel TableLayoutPanel3;
        internal TableLayoutPanel TableLayoutPanel5;

        private KryptonGroup panel_Card; // Đổi sang KryptonGroup
        private ToolTip toolTip1;
    }
}