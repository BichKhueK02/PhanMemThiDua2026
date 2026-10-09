namespace PhanMemThiDua2026
{
    partial class Form3_DangKyTaiKhoan
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

        // ===== BẢNG MÀU (không dùng trắng) =====
        // Nền form      : #CFDBEA   Nền thẻ   : #E6EEF8   Nền ô nhập : #FFF8DC (kem nhạt)
        // Header        : #BBD3F2   Footer    : #BFD0E6
        // Chữ chính     : #0F172A   Chữ nhãn  : #1E293B   Tiêu đề    : #1E3A8A
        // Nút Đăng ký   : nền xanh lá #4ADE80, chữ #052E16
        // Nút Thoát     : nền đỏ nhạt #FCA5A5, chữ #7F1D1D
        // Nút Thêm ảnh  : nền xanh trời #7DD3FC, chữ #0C4A6E

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form3_DangKyTaiKhoan));

            toolTip1 = new ToolTip(components);

            pnlHeader = new Panel();
            pnlHeaderLine = new Panel();
            pictureBox2_AnhDaiDienAdmin = new PictureBox();
            kryptonButton1_ThemAnhDaiDien = new Krypton.Toolkit.KryptonButton();
            label2 = new Label();
            label1 = new Label();

            pnlFooter = new Panel();
            pictureBox2_ThongTin_DKTK = new PictureBox();
            btn_Thoat = new Krypton.Toolkit.KryptonButton();
            btn_Luu = new Krypton.Toolkit.KryptonButton();

            tblBody = new TableLayoutPanel();

            pnlCardTrai = new Panel();
            pnlStripeTrai = new Panel();
            tblTrai = new TableLayoutPanel();
            lblTieuDeTrai = new Label();
            Label4 = new Label();
            text_TaiKhoanMoi = new Krypton.Toolkit.KryptonTextBox();
            Label3 = new Label();
            text_MatKhauMoi = new Krypton.Toolkit.KryptonTextBox();
            Label5 = new Label();
            text_NhapLaiMatKhau = new Krypton.Toolkit.KryptonTextBox();
            Label9 = new Label();
            tblToken = new TableLayoutPanel();
            text_Token = new Krypton.Toolkit.KryptonTextBox();
            PictureBox1 = new PictureBox();
            Check_HienMatKhau = new CheckBox();

            pnlCardPhai = new Panel();
            pnlStripePhai = new Panel();
            tblPhai = new TableLayoutPanel();
            Label6 = new Label();
            Label7 = new Label();
            ComboBox1_CauHoi1 = new ComboBox();
            label10 = new Label();
            TextBox1_CauTraLoi1 = new Krypton.Toolkit.KryptonTextBox();
            Label8 = new Label();
            ComboBox2_CauHoi2 = new ComboBox();
            label11 = new Label();
            TextBox2_CauTraLoi2 = new Krypton.Toolkit.KryptonTextBox();

            pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2_AnhDaiDienAdmin).BeginInit();
            pnlFooter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2_ThongTin_DKTK).BeginInit();
            tblBody.SuspendLayout();
            pnlCardTrai.SuspendLayout();
            tblTrai.SuspendLayout();
            tblToken.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)PictureBox1).BeginInit();
            pnlCardPhai.SuspendLayout();
            tblPhai.SuspendLayout();
            SuspendLayout();

            // ===================== HEADER =====================
            pnlHeader.BackColor = Color.FromArgb(187, 211, 242);
            pnlHeader.Controls.Add(label1);
            pnlHeader.Controls.Add(label2);
            pnlHeader.Controls.Add(kryptonButton1_ThemAnhDaiDien);
            pnlHeader.Controls.Add(pictureBox2_AnhDaiDienAdmin);
            pnlHeader.Controls.Add(pnlHeaderLine);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(1000, 140);
            pnlHeader.TabIndex = 0;
            //
            // pnlHeaderLine (đường nhấn dưới header)
            //
            pnlHeaderLine.BackColor = Color.FromArgb(37, 99, 235);
            pnlHeaderLine.Dock = DockStyle.Bottom;
            pnlHeaderLine.Name = "pnlHeaderLine";
            pnlHeaderLine.Size = new Size(1000, 5);
            pnlHeaderLine.TabIndex = 4;
            //
            // pictureBox2_AnhDaiDienAdmin
            //
            pictureBox2_AnhDaiDienAdmin.BackColor = Color.FromArgb(230, 238, 248);
            pictureBox2_AnhDaiDienAdmin.BorderStyle = BorderStyle.FixedSingle;
            pictureBox2_AnhDaiDienAdmin.Image = (Image)resources.GetObject("pictureBox2_AnhDaiDienAdmin.Image");
            pictureBox2_AnhDaiDienAdmin.Location = new Point(40, 20);
            pictureBox2_AnhDaiDienAdmin.Name = "pictureBox2_AnhDaiDienAdmin";
            pictureBox2_AnhDaiDienAdmin.Size = new Size(96, 96);
            pictureBox2_AnhDaiDienAdmin.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox2_AnhDaiDienAdmin.TabIndex = 0;
            pictureBox2_AnhDaiDienAdmin.TabStop = false;
            //
            // kryptonButton1_ThemAnhDaiDien
            //
            kryptonButton1_ThemAnhDaiDien.Location = new Point(160, 82);
            kryptonButton1_ThemAnhDaiDien.Name = "kryptonButton1_ThemAnhDaiDien";
            kryptonButton1_ThemAnhDaiDien.Size = new Size(130, 34);
            kryptonButton1_ThemAnhDaiDien.TabIndex = 1;
            StyleButton(kryptonButton1_ThemAnhDaiDien,
                Color.FromArgb(125, 211, 252), Color.FromArgb(56, 189, 248), Color.FromArgb(14, 165, 233),
                Color.FromArgb(3, 105, 161), Color.FromArgb(12, 74, 110), 17F, 10F);
            kryptonButton1_ThemAnhDaiDien.Values.DropDownArrowColor = Color.Empty;
            kryptonButton1_ThemAnhDaiDien.Values.Image = (Image)resources.GetObject("kryptonButton1_ThemAnhDaiDien.Values.Image");
            kryptonButton1_ThemAnhDaiDien.Values.Text = "Thêm ảnh";
            kryptonButton1_ThemAnhDaiDien.Click += kryptonButton1_ThemAnhDaiDien_Click;
            //
            // label2 (tiêu đề)
            //
            label2.AutoSize = true;
            label2.BackColor = Color.Transparent;
            label2.Font = new Font("Segoe UI", 22F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.FromArgb(30, 58, 138);
            label2.Location = new Point(156, 18);
            label2.Name = "label2";
            label2.Size = new Size(400, 41);
            label2.TabIndex = 2;
            label2.Text = "Đăng ký tài khoản mới";
            //
            // label1 (phụ đề)
            //
            label1.AutoSize = true;
            label1.BackColor = Color.Transparent;
            label1.Font = new Font("Segoe UI", 11F);
            label1.ForeColor = Color.FromArgb(30, 41, 59);
            label1.Location = new Point(160, 56);
            label1.Name = "label1";
            label1.Size = new Size(300, 20);
            label1.TabIndex = 3;
            label1.Text = "Nhập thông tin tài khoản mới của bạn";

            // ===================== FOOTER =====================
            pnlFooter.BackColor = Color.FromArgb(191, 208, 230);
            pnlFooter.Controls.Add(btn_Luu);
            pnlFooter.Controls.Add(btn_Thoat);
            pnlFooter.Controls.Add(pictureBox2_ThongTin_DKTK);
            pnlFooter.Dock = DockStyle.Bottom;
            pnlFooter.Location = new Point(0, 590);
            pnlFooter.Name = "pnlFooter";
            pnlFooter.Size = new Size(1000, 70);
            pnlFooter.TabIndex = 2;
            //
            // pictureBox2_ThongTin_DKTK
            //
            pictureBox2_ThongTin_DKTK.BackColor = Color.Transparent;
            pictureBox2_ThongTin_DKTK.Cursor = Cursors.Hand;
            pictureBox2_ThongTin_DKTK.Image = (Image)resources.GetObject("pictureBox2_ThongTin_DKTK.Image");
            pictureBox2_ThongTin_DKTK.Location = new Point(40, 17);
            pictureBox2_ThongTin_DKTK.Name = "pictureBox2_ThongTin_DKTK";
            pictureBox2_ThongTin_DKTK.Size = new Size(36, 36);
            pictureBox2_ThongTin_DKTK.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox2_ThongTin_DKTK.TabIndex = 2;
            pictureBox2_ThongTin_DKTK.TabStop = false;
            pictureBox2_ThongTin_DKTK.Click += pictureBox2_ThongTin_DKTK_Click;
            //
            // btn_Thoat (đỏ nhạt, chữ đỏ sẫm)
            //
            btn_Thoat.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btn_Thoat.Location = new Point(650, 15);
            btn_Thoat.Name = "btn_Thoat";
            btn_Thoat.Size = new Size(150, 42);
            btn_Thoat.TabIndex = 1;
            StyleButton(btn_Thoat,
                Color.FromArgb(252, 165, 165), Color.FromArgb(248, 113, 113), Color.FromArgb(239, 68, 68),
                Color.FromArgb(153, 27, 27), Color.FromArgb(127, 29, 29), 10F, 11F);
            btn_Thoat.Values.DropDownArrowColor = Color.Empty;
            btn_Thoat.Values.Image = (Image)resources.GetObject("btn_Thoat.Values.Image");
            btn_Thoat.Values.Text = "Thoát";
            btn_Thoat.Click += btn_Thoat_Click;
            //
            // btn_Luu (xanh lá, chữ xanh rất đậm)
            //
            btn_Luu.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btn_Luu.Location = new Point(815, 15);
            btn_Luu.Name = "btn_Luu";
            btn_Luu.Size = new Size(150, 42);
            btn_Luu.TabIndex = 0;
            StyleButton(btn_Luu,
                Color.FromArgb(74, 222, 128), Color.FromArgb(34, 197, 94), Color.FromArgb(22, 163, 74),
                Color.FromArgb(21, 128, 61), Color.FromArgb(5, 46, 22), 10F, 11F);
            btn_Luu.Values.DropDownArrowColor = Color.Empty;
            btn_Luu.Values.Image = (Image)resources.GetObject("btn_Luu.Values.Image");
            btn_Luu.Values.Text = "Đăng ký";
            btn_Luu.Click += btn_Luu_Click;

            // ===================== BODY (2 thẻ) =====================
            tblBody.BackColor = Color.FromArgb(207, 219, 234);
            tblBody.ColumnCount = 2;
            tblBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tblBody.Controls.Add(pnlCardTrai, 0, 0);
            tblBody.Controls.Add(pnlCardPhai, 1, 0);
            tblBody.Dock = DockStyle.Fill;
            tblBody.Location = new Point(0, 140);
            tblBody.Name = "tblBody";
            tblBody.Padding = new Padding(32, 24, 32, 16);
            tblBody.RowCount = 1;
            tblBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblBody.Size = new Size(1000, 450);
            tblBody.TabIndex = 1;

            // ---------- Thẻ trái ----------
            pnlCardTrai.BackColor = Color.FromArgb(230, 238, 248);
            pnlCardTrai.BorderStyle = BorderStyle.FixedSingle;
            pnlCardTrai.Controls.Add(tblTrai);
            pnlCardTrai.Controls.Add(lblTieuDeTrai);
            pnlCardTrai.Controls.Add(pnlStripeTrai);
            pnlCardTrai.Dock = DockStyle.Fill;
            pnlCardTrai.Margin = new Padding(0, 0, 12, 0);
            pnlCardTrai.Name = "pnlCardTrai";
            pnlCardTrai.Padding = new Padding(24, 12, 24, 8);
            pnlCardTrai.TabIndex = 0;
            //
            // pnlStripeTrai (vạch màu đầu thẻ)
            //
            pnlStripeTrai.BackColor = Color.FromArgb(37, 99, 235);
            pnlStripeTrai.Dock = DockStyle.Top;
            pnlStripeTrai.Name = "pnlStripeTrai";
            pnlStripeTrai.Size = new Size(400, 6);
            pnlStripeTrai.TabIndex = 2;
            //
            // lblTieuDeTrai
            //
            lblTieuDeTrai.Dock = DockStyle.Top;
            lblTieuDeTrai.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblTieuDeTrai.ForeColor = Color.FromArgb(30, 58, 138);
            lblTieuDeTrai.Name = "lblTieuDeTrai";
            lblTieuDeTrai.Size = new Size(400, 36);
            lblTieuDeTrai.TabIndex = 0;
            lblTieuDeTrai.Text = "Thông tin đăng nhập";
            lblTieuDeTrai.TextAlign = ContentAlignment.MiddleLeft;
            //
            // tblTrai
            //
            tblTrai.BackColor = Color.Transparent;
            tblTrai.ColumnCount = 1;
            tblTrai.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblTrai.Controls.Add(Label4, 0, 0);
            tblTrai.Controls.Add(text_TaiKhoanMoi, 0, 1);
            tblTrai.Controls.Add(Label3, 0, 2);
            tblTrai.Controls.Add(text_MatKhauMoi, 0, 3);
            tblTrai.Controls.Add(Label5, 0, 4);
            tblTrai.Controls.Add(text_NhapLaiMatKhau, 0, 5);
            tblTrai.Controls.Add(Label9, 0, 6);
            tblTrai.Controls.Add(tblToken, 0, 7);
            tblTrai.Controls.Add(Check_HienMatKhau, 0, 8);
            tblTrai.Dock = DockStyle.Fill;
            tblTrai.Name = "tblTrai";
            tblTrai.RowCount = 9;
            tblTrai.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tblTrai.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            tblTrai.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tblTrai.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            tblTrai.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tblTrai.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            tblTrai.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tblTrai.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            tblTrai.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblTrai.TabIndex = 1;
            //
            // Các nhãn + ô nhập thẻ trái
            //
            StyleLabel(Label4, "Label4", "Tên tài khoản mới", 0);
            ConfigInput(text_TaiKhoanMoi, "text_TaiKhoanMoi", 1);
            StyleLabel(Label3, "Label3", "Nhập mật khẩu mới", 2);
            ConfigInput(text_MatKhauMoi, "text_MatKhauMoi", 3);
            StyleLabel(Label5, "Label5", "Nhập lại mật khẩu", 4);
            ConfigInput(text_NhapLaiMatKhau, "text_NhapLaiMatKhau", 5);
            StyleLabel(Label9, "Label9", "Token CSDL", 6);
            //
            // tblToken
            //
            tblToken.BackColor = Color.Transparent;
            tblToken.ColumnCount = 2;
            tblToken.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblToken.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44F));
            tblToken.Controls.Add(text_Token, 0, 0);
            tblToken.Controls.Add(PictureBox1, 1, 0);
            tblToken.Dock = DockStyle.Fill;
            tblToken.Margin = new Padding(0);
            tblToken.Name = "tblToken";
            tblToken.RowCount = 1;
            tblToken.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblToken.TabIndex = 7;
            ConfigInput(text_Token, "text_Token", 0);
            //
            // PictureBox1 (biểu tượng thông tin token)
            //
            PictureBox1.Anchor = AnchorStyles.None;
            PictureBox1.BackColor = Color.Transparent;
            PictureBox1.Cursor = Cursors.Hand;
            PictureBox1.Image = (Image)resources.GetObject("PictureBox1.Image");
            PictureBox1.Name = "PictureBox1";
            PictureBox1.Size = new Size(32, 32);
            PictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            PictureBox1.TabIndex = 1;
            PictureBox1.TabStop = false;
            PictureBox1.Click += PictureBox1_Click;
            //
            // Check_HienMatKhau
            //
            Check_HienMatKhau.Anchor = AnchorStyles.Left;
            Check_HienMatKhau.AutoSize = true;
            Check_HienMatKhau.BackColor = Color.Transparent;
            Check_HienMatKhau.Cursor = Cursors.Hand;
            Check_HienMatKhau.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            Check_HienMatKhau.ForeColor = Color.FromArgb(30, 64, 175);
            Check_HienMatKhau.Name = "Check_HienMatKhau";
            Check_HienMatKhau.TabIndex = 8;
            Check_HienMatKhau.Text = "Hiện mật khẩu";
            Check_HienMatKhau.UseVisualStyleBackColor = false;

            // ---------- Thẻ phải ----------
            pnlCardPhai.BackColor = Color.FromArgb(230, 238, 248);
            pnlCardPhai.BorderStyle = BorderStyle.FixedSingle;
            pnlCardPhai.Controls.Add(tblPhai);
            pnlCardPhai.Controls.Add(Label6);
            pnlCardPhai.Controls.Add(pnlStripePhai);
            pnlCardPhai.Dock = DockStyle.Fill;
            pnlCardPhai.Margin = new Padding(12, 0, 0, 0);
            pnlCardPhai.Name = "pnlCardPhai";
            pnlCardPhai.Padding = new Padding(24, 12, 24, 8);
            pnlCardPhai.TabIndex = 1;
            //
            // pnlStripePhai
            //
            pnlStripePhai.BackColor = Color.FromArgb(22, 163, 74);
            pnlStripePhai.Dock = DockStyle.Top;
            pnlStripePhai.Name = "pnlStripePhai";
            pnlStripePhai.Size = new Size(400, 6);
            pnlStripePhai.TabIndex = 2;
            //
            // Label6 (tiêu đề thẻ phải)
            //
            Label6.Dock = DockStyle.Top;
            Label6.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            Label6.ForeColor = Color.FromArgb(20, 83, 45);
            Label6.Name = "Label6";
            Label6.Size = new Size(400, 36);
            Label6.TabIndex = 0;
            Label6.Text = "Câu hỏi bảo mật";
            Label6.TextAlign = ContentAlignment.MiddleLeft;
            //
            // tblPhai
            //
            tblPhai.BackColor = Color.Transparent;
            tblPhai.ColumnCount = 1;
            tblPhai.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblPhai.Controls.Add(Label7, 0, 0);
            tblPhai.Controls.Add(ComboBox1_CauHoi1, 0, 1);
            tblPhai.Controls.Add(label10, 0, 2);
            tblPhai.Controls.Add(TextBox1_CauTraLoi1, 0, 3);
            tblPhai.Controls.Add(Label8, 0, 4);
            tblPhai.Controls.Add(ComboBox2_CauHoi2, 0, 5);
            tblPhai.Controls.Add(label11, 0, 6);
            tblPhai.Controls.Add(TextBox2_CauTraLoi2, 0, 7);
            tblPhai.Dock = DockStyle.Fill;
            tblPhai.Name = "tblPhai";
            tblPhai.RowCount = 9;
            tblPhai.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tblPhai.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            tblPhai.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tblPhai.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            tblPhai.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tblPhai.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            tblPhai.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tblPhai.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            tblPhai.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblPhai.TabIndex = 1;
            StyleLabel(Label7, "Label7", "Câu hỏi 1", 0);
            ConfigCombo(ComboBox1_CauHoi1, "ComboBox1_CauHoi1", 1);
            StyleLabel(label10, "label10", "Câu trả lời 1", 2);
            ConfigInput(TextBox1_CauTraLoi1, "TextBox1_CauTraLoi1", 3);
            StyleLabel(Label8, "Label8", "Câu hỏi 2", 4);
            ConfigCombo(ComboBox2_CauHoi2, "ComboBox2_CauHoi2", 5);
            StyleLabel(label11, "label11", "Câu trả lời 2", 6);
            ConfigInput(TextBox2_CauTraLoi2, "TextBox2_CauTraLoi2", 7);

            // ===================== FORM =====================
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(207, 219, 234);
            ClientSize = new Size(1000, 660);
            Controls.Add(tblBody);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(900, 640);
            Name = "Form3_DangKyTaiKhoan";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Tạo tài khoản";
            Load += Form3_Load;

            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2_AnhDaiDienAdmin).EndInit();
            pnlFooter.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox2_ThongTin_DKTK).EndInit();
            tblBody.ResumeLayout(false);
            pnlCardTrai.ResumeLayout(false);
            tblTrai.ResumeLayout(false);
            tblTrai.PerformLayout();
            tblToken.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)PictureBox1).EndInit();
            pnlCardPhai.ResumeLayout(false);
            tblPhai.ResumeLayout(false);
            tblPhai.PerformLayout();
            ResumeLayout(false);
        }

        // Nút phẳng: nền màu, chữ đậm tối màu, đổi màu khi rê chuột / nhấn
        private void StyleButton(Krypton.Toolkit.KryptonButton b, Color back, Color hover, Color press,
                                 Color border, Color text, float rounding, float fontSize)
        {
            var solid = Krypton.Toolkit.PaletteColorStyle.Solid;
            var font = new Font("Segoe UI Semibold", fontSize, FontStyle.Bold);

            b.StateCommon.Back.Color1 = back;
            b.StateCommon.Back.Color2 = back;
            b.StateCommon.Back.ColorStyle = solid;
            b.StateCommon.Border.Color1 = border;
            b.StateCommon.Border.Color2 = border;
            b.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            b.StateCommon.Border.Rounding = rounding;
            b.StateCommon.Border.Width = 2;
            b.StateCommon.Content.ShortText.Color1 = text;
            b.StateCommon.Content.ShortText.Color2 = text;
            b.StateCommon.Content.ShortText.Font = font;

            b.StateNormal.Back.Color1 = back;
            b.StateNormal.Back.Color2 = back;
            b.StateNormal.Content.ShortText.Color1 = text;

            b.StateTracking.Back.Color1 = hover;
            b.StateTracking.Back.Color2 = hover;
            b.StateTracking.Border.Color1 = border;
            b.StateTracking.Content.ShortText.Color1 = text;

            b.StatePressed.Back.Color1 = press;
            b.StatePressed.Back.Color2 = press;
            b.StatePressed.Border.Color1 = border;
            b.StatePressed.Content.ShortText.Color1 = text;
        }

        // Ô nhập: nền kem nhạt, viền xám đậm, chữ gần đen; focus viền xanh
        private void ConfigInput(Krypton.Toolkit.KryptonTextBox tb, string name, int tabIndex)
        {
            Color cream = Color.FromArgb(255, 248, 220);
            Color ink = Color.FromArgb(15, 23, 42);

            tb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tb.Margin = new Padding(0, 3, 0, 3);
            tb.Name = name;
            tb.Size = new Size(380, 36);
            tb.TabIndex = tabIndex;

            tb.StateCommon.Back.Color1 = cream;
            tb.StateCommon.Border.Color1 = Color.FromArgb(71, 85, 105);
            tb.StateCommon.Border.Rounding = 8F;
            tb.StateCommon.Border.Width = 2;
            tb.StateCommon.Content.Font = new Font("Segoe UI", 10.5F);
            tb.StateCommon.Content.Color1 = ink;

            tb.StateNormal.Back.Color1 = cream;
            tb.StateNormal.Content.Color1 = ink;
            tb.StateActive.Back.Color1 = cream;
            tb.StateActive.Border.Color1 = Color.FromArgb(37, 99, 235);
            tb.StateActive.Content.Color1 = ink;
        }

        private void ConfigCombo(ComboBox cb, string name, int tabIndex)
        {
            cb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            cb.FlatStyle = FlatStyle.Flat;
            cb.BackColor = Color.FromArgb(255, 248, 220);
            cb.Font = new Font("Segoe UI", 10.5F);
            cb.ForeColor = Color.FromArgb(15, 23, 42);
            cb.FormattingEnabled = true;
            cb.Margin = new Padding(0, 6, 0, 3);
            cb.Name = name;
            cb.Size = new Size(380, 28);
            cb.TabIndex = tabIndex;
        }

        private void StyleLabel(Label lb, string name, string text, int tabIndex)
        {
            lb.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            lb.AutoSize = true;
            lb.BackColor = Color.Transparent;
            lb.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            lb.ForeColor = Color.FromArgb(30, 41, 59);
            lb.Margin = new Padding(0, 0, 0, 2);
            lb.Name = name;
            lb.TabIndex = tabIndex;
            lb.Text = text;
        }

        #endregion

        private ToolTip toolTip1;

        private Panel pnlHeader;
        private Panel pnlHeaderLine;
        internal PictureBox pictureBox2_AnhDaiDienAdmin;
        internal Krypton.Toolkit.KryptonButton kryptonButton1_ThemAnhDaiDien;
        internal Label label2;
        internal Label label1;

        private Panel pnlFooter;
        internal PictureBox pictureBox2_ThongTin_DKTK;
        internal Krypton.Toolkit.KryptonButton btn_Thoat;
        internal Krypton.Toolkit.KryptonButton btn_Luu;

        private TableLayoutPanel tblBody;

        private Panel pnlCardTrai;
        private Panel pnlStripeTrai;
        private TableLayoutPanel tblTrai;
        private Label lblTieuDeTrai;
        internal Label Label4;
        private Krypton.Toolkit.KryptonTextBox text_TaiKhoanMoi;
        internal Label Label3;
        private Krypton.Toolkit.KryptonTextBox text_MatKhauMoi;
        internal Label Label5;
        private Krypton.Toolkit.KryptonTextBox text_NhapLaiMatKhau;
        internal Label Label9;
        private TableLayoutPanel tblToken;
        private Krypton.Toolkit.KryptonTextBox text_Token;
        internal PictureBox PictureBox1;
        internal CheckBox Check_HienMatKhau;

        private Panel pnlCardPhai;
        private Panel pnlStripePhai;
        private TableLayoutPanel tblPhai;
        internal Label Label6;
        internal Label Label7;
        internal ComboBox ComboBox1_CauHoi1;
        internal Label label10;
        private Krypton.Toolkit.KryptonTextBox TextBox1_CauTraLoi1;
        internal Label Label8;
        internal ComboBox ComboBox2_CauHoi2;
        internal Label label11;
        private Krypton.Toolkit.KryptonTextBox TextBox2_CauTraLoi2;
    }
}