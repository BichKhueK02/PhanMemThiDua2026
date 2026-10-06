using Krypton.Toolkit;

namespace PhanMemThiDua2026
{
    partial class Form39_ThongTinNguoiDung
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form39_ThongTinNguoiDung));
            panelHeader = new Panel();
            lblTieuDe = new Label();
            lblMoTa = new Label();
            pictureBox2_AnhDaiDienAdmin = new PictureBox();
            panelFooter = new Panel();
            kryptonButton1_DongFrom = new Krypton.Toolkit.KryptonButton();
            panelBody = new Panel();
            panelCard = new Panel();
            tableThongTin = new TableLayoutPanel();
            label9 = new Label();
            textBox_TenTaiKhoan = new Krypton.Toolkit.KryptonTextBox();
            label10 = new Label();
            textBox_ThoiGianDangNhap = new Krypton.Toolkit.KryptonTextBox();
            label13 = new Label();
            textBox_TenUserWindows = new Krypton.Toolkit.KryptonTextBox();
            label12 = new Label();
            textBox_TenMayTinh = new Krypton.Toolkit.KryptonTextBox();
            label2 = new Label();
            textbox_SeriaMay = new Krypton.Toolkit.KryptonTextBox();
            label15 = new Label();
            kryptonTextBox1_TheLoaiMayTinh = new Krypton.Toolkit.KryptonTextBox();
            label14 = new Label();
            textBox_PhienbanPhanMem = new Krypton.Toolkit.KryptonTextBox();
            label1 = new Label();
            kryptonTextBox1_CapNhatLanCuoi = new Krypton.Toolkit.KryptonTextBox();
            toolTip1 = new ToolTip(components);
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2_AnhDaiDienAdmin).BeginInit();
            panelFooter.SuspendLayout();
            panelBody.SuspendLayout();
            panelCard.SuspendLayout();
            tableThongTin.SuspendLayout();
            SuspendLayout();
            //
            // panelHeader  (dải banner xanh phía trên)
            //
            panelHeader.BackColor = Color.FromArgb(30, 64, 175);
            panelHeader.Controls.Add(lblMoTa);
            panelHeader.Controls.Add(lblTieuDe);
            panelHeader.Controls.Add(pictureBox2_AnhDaiDienAdmin);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(760, 130);
            panelHeader.TabIndex = 2;
            //
            // pictureBox2_AnhDaiDienAdmin  (ảnh đại diện - bo tròn trong Load)
            //
            pictureBox2_AnhDaiDienAdmin.BackColor = Color.White;
            pictureBox2_AnhDaiDienAdmin.Image = (Image)resources.GetObject("pictureBox2_AnhDaiDienAdmin.Image");
            pictureBox2_AnhDaiDienAdmin.Location = new Point(32, 17);
            pictureBox2_AnhDaiDienAdmin.Name = "pictureBox2_AnhDaiDienAdmin";
            pictureBox2_AnhDaiDienAdmin.Size = new Size(96, 96);
            pictureBox2_AnhDaiDienAdmin.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox2_AnhDaiDienAdmin.TabIndex = 0;
            pictureBox2_AnhDaiDienAdmin.TabStop = false;
            //
            // lblTieuDe
            //
            lblTieuDe.AutoSize = true;
            lblTieuDe.BackColor = Color.Transparent;
            lblTieuDe.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold);
            lblTieuDe.ForeColor = Color.White;
            lblTieuDe.Location = new Point(150, 36);
            lblTieuDe.Name = "lblTieuDe";
            lblTieuDe.Size = new Size(290, 31);
            lblTieuDe.TabIndex = 1;
            lblTieuDe.Text = "Thông tin người sử dụng";
            //
            // lblMoTa
            //
            lblMoTa.AutoSize = true;
            lblMoTa.BackColor = Color.Transparent;
            lblMoTa.Font = new Font("Segoe UI", 10F);
            lblMoTa.ForeColor = Color.FromArgb(191, 219, 254);
            lblMoTa.Location = new Point(152, 72);
            lblMoTa.Name = "lblMoTa";
            lblMoTa.Size = new Size(300, 19);
            lblMoTa.TabIndex = 2;
            lblMoTa.Text = "Tài khoản đăng nhập và thông tin thiết bị";
            //
            // panelFooter
            //
            panelFooter.BackColor = Color.FromArgb(243, 244, 246);
            panelFooter.Controls.Add(kryptonButton1_DongFrom);
            panelFooter.Dock = DockStyle.Bottom;
            panelFooter.Location = new Point(0, 456);
            panelFooter.Name = "panelFooter";
            panelFooter.Size = new Size(760, 64);
            panelFooter.TabIndex = 1;
            //
            // kryptonButton1_DongFrom

            kryptonButton1_DongFrom.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            kryptonButton1_DongFrom.Cursor = Cursors.Hand;
            kryptonButton1_DongFrom.Location = new Point(600, 8);
            kryptonButton1_DongFrom.Name = "kryptonButton1_DongFrom";
            kryptonButton1_DongFrom.Size = new Size(128, 40);

            // Màu mặc định của Krypton
            kryptonButton1_DongFrom.OverrideDefault.Back.ColorStyle = PaletteColorStyle.Solid;
            kryptonButton1_DongFrom.OverrideDefault.Back.Color1 = Color.FromArgb(37, 99, 235);
            kryptonButton1_DongFrom.OverrideDefault.Back.Color2 = Color.FromArgb(37, 99, 235);
            kryptonButton1_DongFrom.OverrideDefault.Border.Color1 = Color.FromArgb(37, 99, 235);
            kryptonButton1_DongFrom.OverrideDefault.Border.Color2 = Color.FromArgb(37, 99, 235);
            kryptonButton1_DongFrom.OverrideDefault.Content.ShortText.Color1 = Color.White;
            kryptonButton1_DongFrom.OverrideDefault.Content.ShortText.Color2 = Color.White;

            // Trạng thái chung
            kryptonButton1_DongFrom.StateCommon.Back.ColorStyle = PaletteColorStyle.Solid;
            kryptonButton1_DongFrom.StateCommon.Back.Color1 = Color.FromArgb(37, 99, 235);
            kryptonButton1_DongFrom.StateCommon.Back.Color2 = Color.FromArgb(37, 99, 235);
            kryptonButton1_DongFrom.StateCommon.Border.DrawBorders =
                PaletteDrawBorders.Top |
                PaletteDrawBorders.Bottom |
                PaletteDrawBorders.Left |
                PaletteDrawBorders.Right;
            kryptonButton1_DongFrom.StateCommon.Border.Color1 = Color.FromArgb(37, 99, 235);
            kryptonButton1_DongFrom.StateCommon.Border.Color2 = Color.FromArgb(37, 99, 235);
            kryptonButton1_DongFrom.StateCommon.Border.Rounding = 8F;
            kryptonButton1_DongFrom.StateCommon.Border.Width = 1;
            kryptonButton1_DongFrom.StateCommon.Content.ShortText.Color1 = Color.White;
            kryptonButton1_DongFrom.StateCommon.Content.ShortText.Color2 = Color.White;
            kryptonButton1_DongFrom.StateCommon.Content.ShortText.Font =
                new Font("Segoe UI Semibold", 10F, FontStyle.Bold);

            // Trạng thái bình thường
            kryptonButton1_DongFrom.StateNormal.Back.ColorStyle = PaletteColorStyle.Solid;
            kryptonButton1_DongFrom.StateNormal.Back.Color1 = Color.FromArgb(37, 99, 235);
            kryptonButton1_DongFrom.StateNormal.Back.Color2 = Color.FromArgb(37, 99, 235);
            kryptonButton1_DongFrom.StateNormal.Border.Color1 = Color.FromArgb(37, 99, 235);
            kryptonButton1_DongFrom.StateNormal.Border.Color2 = Color.FromArgb(37, 99, 235);
            kryptonButton1_DongFrom.StateNormal.Content.ShortText.Color1 = Color.White;
            kryptonButton1_DongFrom.StateNormal.Content.ShortText.Color2 = Color.White;

            // Hover
            kryptonButton1_DongFrom.StateTracking.Back.ColorStyle = PaletteColorStyle.Solid;
            kryptonButton1_DongFrom.StateTracking.Back.Color1 = Color.FromArgb(29, 78, 216);
            kryptonButton1_DongFrom.StateTracking.Back.Color2 = Color.FromArgb(29, 78, 216);
            kryptonButton1_DongFrom.StateTracking.Border.Color1 = Color.FromArgb(29, 78, 216);
            kryptonButton1_DongFrom.StateTracking.Border.Color2 = Color.FromArgb(29, 78, 216);
            kryptonButton1_DongFrom.StateTracking.Content.ShortText.Color1 = Color.White;
            kryptonButton1_DongFrom.StateTracking.Content.ShortText.Color2 = Color.White;

            // Nhấn
            kryptonButton1_DongFrom.StatePressed.Back.ColorStyle = PaletteColorStyle.Solid;
            kryptonButton1_DongFrom.StatePressed.Back.Color1 = Color.FromArgb(30, 64, 175);
            kryptonButton1_DongFrom.StatePressed.Back.Color2 = Color.FromArgb(30, 64, 175);
            kryptonButton1_DongFrom.StatePressed.Border.Color1 = Color.FromArgb(30, 64, 175);
            kryptonButton1_DongFrom.StatePressed.Border.Color2 = Color.FromArgb(30, 64, 175);
            kryptonButton1_DongFrom.StatePressed.Content.ShortText.Color1 = Color.White;
            kryptonButton1_DongFrom.StatePressed.Content.ShortText.Color2 = Color.White;

            kryptonButton1_DongFrom.TabIndex = 0;
            kryptonButton1_DongFrom.Values.DropDownArrowColor = Color.Empty;
            kryptonButton1_DongFrom.Values.Image =
                (Image)resources.GetObject("kryptonButton1_DongFrom.Values.Image");
            kryptonButton1_DongFrom.Values.Text = "Đóng";
            kryptonButton1_DongFrom.Click += kryptonButton1_DongFrom_Click;

            //
            // panelBody
            //
            panelBody.BackColor = Color.FromArgb(243, 244, 246);
            panelBody.Controls.Add(panelCard);
            panelBody.Dock = DockStyle.Fill;
            panelBody.Location = new Point(0, 130);
            panelBody.Name = "panelBody";
            panelBody.Padding = new Padding(24, 20, 24, 8);
            panelBody.Size = new Size(760, 326);
            panelBody.TabIndex = 0;
            //
            // panelCard  (thẻ trắng chứa thông tin)
            //
            panelCard.BackColor = Color.White;
            panelCard.BorderStyle = BorderStyle.FixedSingle;
            panelCard.Controls.Add(tableThongTin);
            panelCard.Dock = DockStyle.Fill;
            panelCard.Location = new Point(24, 20);
            panelCard.Name = "panelCard";
            panelCard.Padding = new Padding(24, 16, 24, 12);
            panelCard.Size = new Size(712, 298);
            panelCard.TabIndex = 0;
            //
            // tableThongTin  (2 cột x 8 hàng: nhãn / ô nhập xen kẽ)
            //
            tableThongTin.BackColor = Color.White;
            tableThongTin.ColumnCount = 2;
            tableThongTin.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableThongTin.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableThongTin.Controls.Add(label9, 0, 0);
            tableThongTin.Controls.Add(label10, 1, 0);
            tableThongTin.Controls.Add(textBox_TenTaiKhoan, 0, 1);
            tableThongTin.Controls.Add(textBox_ThoiGianDangNhap, 1, 1);
            tableThongTin.Controls.Add(label13, 0, 2);
            tableThongTin.Controls.Add(label12, 1, 2);
            tableThongTin.Controls.Add(textBox_TenUserWindows, 0, 3);
            tableThongTin.Controls.Add(textBox_TenMayTinh, 1, 3);
            tableThongTin.Controls.Add(label2, 0, 4);
            tableThongTin.Controls.Add(label15, 1, 4);
            tableThongTin.Controls.Add(textbox_SeriaMay, 0, 5);
            tableThongTin.Controls.Add(kryptonTextBox1_TheLoaiMayTinh, 1, 5);
            tableThongTin.Controls.Add(label14, 0, 6);
            tableThongTin.Controls.Add(label1, 1, 6);
            tableThongTin.Controls.Add(textBox_PhienbanPhanMem, 0, 7);
            tableThongTin.Controls.Add(kryptonTextBox1_CapNhatLanCuoi, 1, 7);
            tableThongTin.Dock = DockStyle.Fill;
            tableThongTin.Location = new Point(24, 16);
            tableThongTin.Name = "tableThongTin";
            tableThongTin.RowCount = 8;
            tableThongTin.RowStyles.Add(new RowStyle(SizeType.Percent, 11F));
            tableThongTin.RowStyles.Add(new RowStyle(SizeType.Percent, 14F));
            tableThongTin.RowStyles.Add(new RowStyle(SizeType.Percent, 11F));
            tableThongTin.RowStyles.Add(new RowStyle(SizeType.Percent, 14F));
            tableThongTin.RowStyles.Add(new RowStyle(SizeType.Percent, 11F));
            tableThongTin.RowStyles.Add(new RowStyle(SizeType.Percent, 14F));
            tableThongTin.RowStyles.Add(new RowStyle(SizeType.Percent, 11F));
            tableThongTin.RowStyles.Add(new RowStyle(SizeType.Percent, 14F));
            tableThongTin.Size = new Size(662, 268);
            tableThongTin.TabIndex = 0;
            //
            // Nhãn (chữ nhỏ, xám, nằm trên ô nhập)
            //
            label9.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            label9.AutoSize = true;
            label9.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label9.ForeColor = Color.FromArgb(107, 114, 128);
            label9.Name = "label9";
            label9.TabIndex = 0;
            label9.Text = "TÀI KHOẢN";

            label10.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            label10.AutoSize = true;
            label10.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label10.ForeColor = Color.FromArgb(107, 114, 128);
            label10.Name = "label10";
            label10.TabIndex = 1;
            label10.Text = "ĐĂNG NHẬP LÚC";

            label13.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            label13.AutoSize = true;
            label13.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label13.ForeColor = Color.FromArgb(107, 114, 128);
            label13.Name = "label13";
            label13.TabIndex = 4;
            label13.Text = "USER WINDOWS";

            label12.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            label12.AutoSize = true;
            label12.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label12.ForeColor = Color.FromArgb(107, 114, 128);
            label12.Name = "label12";
            label12.TabIndex = 3;
            label12.Text = "TÊN MÁY TÍNH";

            label2.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label2.ForeColor = Color.FromArgb(107, 114, 128);
            label2.Name = "label2";
            label2.TabIndex = 38;
            label2.Text = "SERIA MÁY TÍNH";

            label15.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            label15.AutoSize = true;
            label15.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label15.ForeColor = Color.FromArgb(107, 114, 128);
            label15.Name = "label15";
            label15.TabIndex = 35;
            label15.Text = "THỂ LOẠI MÁY";

            label14.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            label14.AutoSize = true;
            label14.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label14.ForeColor = Color.FromArgb(107, 114, 128);
            label14.Name = "label14";
            label14.TabIndex = 5;
            label14.Text = "PHIÊN BẢN PHẦN MỀM";

            label1.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label1.ForeColor = Color.FromArgb(107, 114, 128);
            label1.Name = "label1";
            label1.TabIndex = 24;
            label1.Text = "CẬP NHẬT LẦN CUỐI";
            //
            // Ô nhập (chỉ đọc, nền xám nhạt, viền bo)
            //
            ConfigTextBox(textBox_TenTaiKhoan, "textBox_TenTaiKhoan", 10);
            ConfigTextBox(textBox_ThoiGianDangNhap, "textBox_ThoiGianDangNhap", 11);
            ConfigTextBox(textBox_TenUserWindows, "textBox_TenUserWindows", 12);
            ConfigTextBox(textBox_TenMayTinh, "textBox_TenMayTinh", 13);
            ConfigTextBox(textbox_SeriaMay, "textbox_SeriaMay", 14);
            ConfigTextBox(kryptonTextBox1_TheLoaiMayTinh, "kryptonTextBox1_TheLoaiMayTinh", 15);
            ConfigTextBox(textBox_PhienbanPhanMem, "textBox_PhienbanPhanMem", 16);
            ConfigTextBox(kryptonTextBox1_CapNhatLanCuoi, "kryptonTextBox1_CapNhatLanCuoi", 17);
            //
            // Form39_ThongTinNguoiDung
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(243, 244, 246);
            ClientSize = new Size(760, 520);
            Controls.Add(panelBody);
            Controls.Add(panelFooter);
            Controls.Add(panelHeader);
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "Form39_ThongTinNguoiDung";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Thông tin người sử dụng";
            Load += Form39_ThongTinNguoiDung_Load;
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2_AnhDaiDienAdmin).EndInit();
            panelFooter.ResumeLayout(false);
            panelBody.ResumeLayout(false);
            panelCard.ResumeLayout(false);
            tableThongTin.ResumeLayout(false);
            tableThongTin.PerformLayout();
            ResumeLayout(false);
        }

        private static void ConfigTextBox(Krypton.Toolkit.KryptonTextBox tb, string name, int tabIndex)
        {
            tb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tb.Margin = new Padding(0, 0, 16, 0);
            tb.Name = name;
            tb.ReadOnly = true;
            tb.TabIndex = tabIndex;
            tb.StateCommon.Back.Color1 = Color.FromArgb(249, 250, 251);
            tb.StateCommon.Border.Color1 = Color.FromArgb(209, 213, 219);
            tb.StateCommon.Border.Color2 = Color.FromArgb(209, 213, 219);
            tb.StateCommon.Border.Rounding = 8F;
            tb.StateCommon.Border.Width = 1;
            tb.StateCommon.Content.Color1 = Color.FromArgb(17, 24, 39);
            tb.StateCommon.Content.Font = new Font("Segoe UI", 10.5F);
        }

        #endregion

        private Panel panelHeader;
        private Label lblTieuDe;
        private Label lblMoTa;
        private Panel panelFooter;
        private Panel panelBody;
        private Panel panelCard;
        private TableLayoutPanel tableThongTin;
        internal PictureBox pictureBox2_AnhDaiDienAdmin;
        internal Krypton.Toolkit.KryptonButton kryptonButton1_DongFrom;
        private Krypton.Toolkit.KryptonTextBox textBox_TenTaiKhoan;
        private Krypton.Toolkit.KryptonTextBox textBox_ThoiGianDangNhap;
        private Krypton.Toolkit.KryptonTextBox textBox_TenUserWindows;
        private Krypton.Toolkit.KryptonTextBox textBox_TenMayTinh;
        private Krypton.Toolkit.KryptonTextBox textbox_SeriaMay;
        private Krypton.Toolkit.KryptonTextBox kryptonTextBox1_TheLoaiMayTinh;
        private Krypton.Toolkit.KryptonTextBox textBox_PhienbanPhanMem;
        private Krypton.Toolkit.KryptonTextBox kryptonTextBox1_CapNhatLanCuoi;
        private Label label1;
        private Label label2;
        private Label label9;
        private Label label10;
        private Label label12;
        private Label label13;
        private Label label14;
        private Label label15;
        private ToolTip toolTip1;
    }
}