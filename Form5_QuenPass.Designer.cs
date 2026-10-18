namespace PhanMemThiDua2026
{
    partial class Form5_QuenPass
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
        /// Giao diện kiểu "web": trang nền xám xanh nhạt, thẻ trắng bo góc,
        /// bên trái là panel gradient thương hiệu, bên phải là form (nhãn nằm trên ô nhập).
        /// </summary>
        private void InitializeComponent()
        {
            pnlRoot = new BufferedPanel();
            pnlCard = new WebCardPanel();
            pnlRight = new BufferedPanel();
            tlpForm = new BufferedTableLayoutPanel();
            lblTieuDe = new Label();
            lblMoTa = new Label();
            lblCauHoi1 = new Label();
            pnlCombo1 = new WebFieldPanel();
            ComboBox1_CauHoi1 = new ComboBox();
            TextBox1_TraLoi1 = new Krypton.Toolkit.KryptonTextBox();
            lblCauHoi2 = new Label();
            pnlCombo2 = new WebFieldPanel();
            ComboBox2_CauHoi2 = new ComboBox();
            TextBox2_TraLoi2 = new Krypton.Toolkit.KryptonTextBox();
            Btn_KiemTra = new Krypton.Toolkit.KryptonButton();
            lblKetQua = new Label();
            tlpKetQua = new BufferedTableLayoutPanel();
            lblTaiKhoan = new Label();
            lblMatKhau = new Label();
            Text_TenTaiKhoan = new Krypton.Toolkit.KryptonTextBox();
            Text_MatKhauLayLai = new Krypton.Toolkit.KryptonTextBox();
            pnlHero = new HeroGradientPanel();
            tlpHero = new BufferedTableLayoutPanel();
            pnlBadge = new WebCardPanel();
            PictureBox2 = new PictureBox();
            lblHeroTitle = new Label();
            lblHeroDesc = new Label();
            lblHeroHint = new Label();
            progressBar1 = new ProgressBar();
            statusStrip1 = new StatusStrip();
            label1_PhienBanPhanMem = new ToolStripStatusLabel();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            pnlRoot.SuspendLayout();
            pnlCard.SuspendLayout();
            pnlRight.SuspendLayout();
            tlpForm.SuspendLayout();
            pnlCombo1.SuspendLayout();
            pnlCombo2.SuspendLayout();
            tlpKetQua.SuspendLayout();
            pnlHero.SuspendLayout();
            tlpHero.SuspendLayout();
            pnlBadge.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)PictureBox2).BeginInit();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // pnlRoot
            // 
            pnlRoot.BackColor = Color.FromArgb(238, 242, 249);
            pnlRoot.Controls.Add(pnlCard);
            pnlRoot.Dock = DockStyle.Fill;
            pnlRoot.Location = new Point(0, 0);
            pnlRoot.Name = "pnlRoot";
            pnlRoot.Padding = new Padding(28);
            pnlRoot.Size = new Size(900, 618);
            pnlRoot.TabIndex = 0;
            // 
            // pnlCard
            // 
            pnlCard.BackColor = Color.White;
            pnlCard.Controls.Add(pnlRight);
            pnlCard.Controls.Add(pnlHero);
            pnlCard.CornerRadius = 18;
            pnlCard.Dock = DockStyle.Fill;
            pnlCard.Location = new Point(28, 28);
            pnlCard.Name = "pnlCard";
            pnlCard.Size = new Size(844, 562);
            pnlCard.TabIndex = 0;
            // 
            // pnlRight
            // 
            pnlRight.BackColor = Color.White;
            pnlRight.Controls.Add(tlpForm);
            pnlRight.Dock = DockStyle.Fill;
            pnlRight.Location = new Point(300, 0);
            pnlRight.Name = "pnlRight";
            pnlRight.Size = new Size(544, 562);
            pnlRight.TabIndex = 0;
            // 
            // tlpForm
            // 
            tlpForm.ColumnCount = 1;
            tlpForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpForm.Controls.Add(lblTieuDe, 0, 0);
            tlpForm.Controls.Add(lblMoTa, 0, 1);
            tlpForm.Controls.Add(lblCauHoi1, 0, 2);
            tlpForm.Controls.Add(pnlCombo1, 0, 3);
            tlpForm.Controls.Add(TextBox1_TraLoi1, 0, 4);
            tlpForm.Controls.Add(lblCauHoi2, 0, 5);
            tlpForm.Controls.Add(pnlCombo2, 0, 6);
            tlpForm.Controls.Add(TextBox2_TraLoi2, 0, 7);
            tlpForm.Controls.Add(Btn_KiemTra, 0, 8);
            tlpForm.Controls.Add(lblKetQua, 0, 9);
            tlpForm.Controls.Add(tlpKetQua, 0, 10);
            tlpForm.Dock = DockStyle.Fill;
            tlpForm.Location = new Point(0, 0);
            tlpForm.Name = "tlpForm";
            tlpForm.Padding = new Padding(44, 28, 44, 20);
            tlpForm.RowCount = 12;
            tlpForm.RowStyles.Add(new RowStyle());
            tlpForm.RowStyles.Add(new RowStyle());
            tlpForm.RowStyles.Add(new RowStyle());
            tlpForm.RowStyles.Add(new RowStyle());
            tlpForm.RowStyles.Add(new RowStyle());
            tlpForm.RowStyles.Add(new RowStyle());
            tlpForm.RowStyles.Add(new RowStyle());
            tlpForm.RowStyles.Add(new RowStyle());
            tlpForm.RowStyles.Add(new RowStyle());
            tlpForm.RowStyles.Add(new RowStyle());
            tlpForm.RowStyles.Add(new RowStyle());
            tlpForm.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpForm.Size = new Size(544, 562);
            tlpForm.TabIndex = 0;
            // 
            // lblTieuDe
            // 
            lblTieuDe.AutoSize = true;
            lblTieuDe.Font = new Font("Segoe UI", 22F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTieuDe.ForeColor = Color.FromArgb(15, 23, 42);
            lblTieuDe.Location = new Point(44, 28);
            lblTieuDe.Margin = new Padding(0, 0, 0, 2);
            lblTieuDe.Name = "lblTieuDe";
            lblTieuDe.Size = new Size(246, 41);
            lblTieuDe.TabIndex = 10;
            lblTieuDe.Text = "Quên mật khẩu?";
            // 
            // lblMoTa
            // 
            lblMoTa.AutoSize = true;
            lblMoTa.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblMoTa.ForeColor = Color.FromArgb(100, 116, 139);
            lblMoTa.Location = new Point(44, 71);
            lblMoTa.Margin = new Padding(0, 0, 0, 10);
            lblMoTa.MaximumSize = new Size(450, 0);
            lblMoTa.Name = "lblMoTa";
            lblMoTa.Size = new Size(437, 38);
            lblMoTa.TabIndex = 11;
            lblMoTa.Text = "Hãy xác minh danh tính bằng hai câu hỏi bảo mật bạn đã thiết lập để lấy lại thông tin đăng nhập.";
            // 
            // lblCauHoi1
            // 
            lblCauHoi1.AutoSize = true;
            lblCauHoi1.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblCauHoi1.ForeColor = Color.FromArgb(51, 65, 85);
            lblCauHoi1.Location = new Point(44, 131);
            lblCauHoi1.Margin = new Padding(0, 12, 0, 4);
            lblCauHoi1.Name = "lblCauHoi1";
            lblCauHoi1.Size = new Size(118, 17);
            lblCauHoi1.TabIndex = 12;
            lblCauHoi1.Text = "Câu hỏi bảo mật 1";
            // 
            // pnlCombo1
            // 
            pnlCombo1.BackColor = Color.White;
            pnlCombo1.Controls.Add(ComboBox1_CauHoi1);
            pnlCombo1.Dock = DockStyle.Fill;
            pnlCombo1.Location = new Point(44, 152);
            pnlCombo1.Margin = new Padding(0, 0, 0, 8);
            pnlCombo1.MinimumSize = new Size(0, 36);
            pnlCombo1.Name = "pnlCombo1";
            pnlCombo1.Padding = new Padding(10, 0, 4, 0);
            pnlCombo1.Size = new Size(456, 36);
            pnlCombo1.TabIndex = 0;
            // 
            // ComboBox1_CauHoi1
            // 
            ComboBox1_CauHoi1.BackColor = Color.White;
            ComboBox1_CauHoi1.FlatStyle = FlatStyle.Flat;
            ComboBox1_CauHoi1.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ComboBox1_CauHoi1.ForeColor = Color.FromArgb(15, 23, 42);
            ComboBox1_CauHoi1.FormattingEnabled = true;
            ComboBox1_CauHoi1.Location = new Point(10, 4);
            ComboBox1_CauHoi1.Name = "ComboBox1_CauHoi1";
            ComboBox1_CauHoi1.Size = new Size(442, 27);
            ComboBox1_CauHoi1.TabIndex = 0;
            // 
            // TextBox1_TraLoi1
            // 
            TextBox1_TraLoi1.CueHint.CueHintText = "Nhập câu trả lời của bạn";
            TextBox1_TraLoi1.Dock = DockStyle.Fill;
            TextBox1_TraLoi1.Location = new Point(44, 196);
            TextBox1_TraLoi1.Margin = new Padding(0);
            TextBox1_TraLoi1.Name = "TextBox1_TraLoi1";
            TextBox1_TraLoi1.Size = new Size(456, 23);
            TextBox1_TraLoi1.TabIndex = 1;
            // 
            // lblCauHoi2
            // 
            lblCauHoi2.AutoSize = true;
            lblCauHoi2.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblCauHoi2.ForeColor = Color.FromArgb(51, 65, 85);
            lblCauHoi2.Location = new Point(44, 233);
            lblCauHoi2.Margin = new Padding(0, 14, 0, 4);
            lblCauHoi2.Name = "lblCauHoi2";
            lblCauHoi2.Size = new Size(120, 17);
            lblCauHoi2.TabIndex = 13;
            lblCauHoi2.Text = "Câu hỏi bảo mật 2";
            // 
            // pnlCombo2
            // 
            pnlCombo2.BackColor = Color.White;
            pnlCombo2.Controls.Add(ComboBox2_CauHoi2);
            pnlCombo2.Dock = DockStyle.Fill;
            pnlCombo2.Location = new Point(44, 254);
            pnlCombo2.Margin = new Padding(0, 0, 0, 8);
            pnlCombo2.MinimumSize = new Size(0, 36);
            pnlCombo2.Name = "pnlCombo2";
            pnlCombo2.Padding = new Padding(10, 0, 4, 0);
            pnlCombo2.Size = new Size(456, 36);
            pnlCombo2.TabIndex = 2;
            // 
            // ComboBox2_CauHoi2
            // 
            ComboBox2_CauHoi2.BackColor = Color.White;
            ComboBox2_CauHoi2.FlatStyle = FlatStyle.Flat;
            ComboBox2_CauHoi2.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ComboBox2_CauHoi2.ForeColor = Color.FromArgb(15, 23, 42);
            ComboBox2_CauHoi2.FormattingEnabled = true;
            ComboBox2_CauHoi2.Location = new Point(10, 4);
            ComboBox2_CauHoi2.Name = "ComboBox2_CauHoi2";
            ComboBox2_CauHoi2.Size = new Size(442, 27);
            ComboBox2_CauHoi2.TabIndex = 0;
            // 
            // TextBox2_TraLoi2
            // 
            TextBox2_TraLoi2.CueHint.CueHintText = "Nhập câu trả lời của bạn";
            TextBox2_TraLoi2.Dock = DockStyle.Fill;
            TextBox2_TraLoi2.Location = new Point(44, 298);
            TextBox2_TraLoi2.Margin = new Padding(0);
            TextBox2_TraLoi2.Name = "TextBox2_TraLoi2";
            TextBox2_TraLoi2.Size = new Size(456, 23);
            TextBox2_TraLoi2.TabIndex = 3;
            // 
            // Btn_KiemTra
            // 
            Btn_KiemTra.Cursor = Cursors.Hand;
            Btn_KiemTra.Dock = DockStyle.Fill;
            Btn_KiemTra.Location = new Point(44, 343);
            Btn_KiemTra.Margin = new Padding(0, 22, 0, 0);
            Btn_KiemTra.MinimumSize = new Size(0, 46);
            Btn_KiemTra.Name = "Btn_KiemTra";
            Btn_KiemTra.Size = new Size(456, 46);
            Btn_KiemTra.TabIndex = 4;
            Btn_KiemTra.Values.DropDownArrowColor = Color.Empty;
            Btn_KiemTra.Values.Text = "Xác minh && lấy lại mật khẩu";
            Btn_KiemTra.Click += Btn_KiemTra_Click;
            // 
            // lblKetQua
            // 
            lblKetQua.AutoSize = true;
            lblKetQua.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblKetQua.ForeColor = Color.FromArgb(100, 116, 139);
            lblKetQua.Location = new Point(44, 411);
            lblKetQua.Margin = new Padding(0, 22, 0, 6);
            lblKetQua.Name = "lblKetQua";
            lblKetQua.Size = new Size(138, 15);
            lblKetQua.TabIndex = 14;
            lblKetQua.Text = "THÔNG TIN KHÔI PHỤC";
            // 
            // tlpKetQua
            // 
            tlpKetQua.AutoSize = true;
            tlpKetQua.ColumnCount = 2;
            tlpKetQua.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpKetQua.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpKetQua.Controls.Add(lblTaiKhoan, 0, 0);
            tlpKetQua.Controls.Add(lblMatKhau, 1, 0);
            tlpKetQua.Controls.Add(Text_TenTaiKhoan, 0, 1);
            tlpKetQua.Controls.Add(Text_MatKhauLayLai, 1, 1);
            tlpKetQua.Dock = DockStyle.Fill;
            tlpKetQua.Location = new Point(44, 432);
            tlpKetQua.Margin = new Padding(0);
            tlpKetQua.Name = "tlpKetQua";
            tlpKetQua.RowCount = 2;
            tlpKetQua.RowStyles.Add(new RowStyle());
            tlpKetQua.RowStyles.Add(new RowStyle());
            tlpKetQua.Size = new Size(456, 44);
            tlpKetQua.TabIndex = 5;
            // 
            // lblTaiKhoan
            // 
            lblTaiKhoan.AutoSize = true;
            lblTaiKhoan.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTaiKhoan.ForeColor = Color.FromArgb(51, 65, 85);
            lblTaiKhoan.Location = new Point(0, 0);
            lblTaiKhoan.Margin = new Padding(0, 0, 0, 4);
            lblTaiKhoan.Name = "lblTaiKhoan";
            lblTaiKhoan.Size = new Size(90, 17);
            lblTaiKhoan.TabIndex = 0;
            lblTaiKhoan.Text = "Tên tài khoản";
            // 
            // lblMatKhau
            // 
            lblMatKhau.AutoSize = true;
            lblMatKhau.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblMatKhau.ForeColor = Color.FromArgb(51, 65, 85);
            lblMatKhau.Location = new Point(234, 0);
            lblMatKhau.Margin = new Padding(6, 0, 0, 4);
            lblMatKhau.Name = "lblMatKhau";
            lblMatKhau.Size = new Size(118, 17);
            lblMatKhau.TabIndex = 1;
            lblMatKhau.Text = "Mật khẩu của bạn";
            // 
            // Text_TenTaiKhoan
            // 
            Text_TenTaiKhoan.Dock = DockStyle.Fill;
            Text_TenTaiKhoan.Location = new Point(0, 21);
            Text_TenTaiKhoan.Margin = new Padding(0, 0, 6, 0);
            Text_TenTaiKhoan.Name = "Text_TenTaiKhoan";
            Text_TenTaiKhoan.Size = new Size(222, 23);
            Text_TenTaiKhoan.TabIndex = 2;
            Text_TenTaiKhoan.TabStop = false;
            // 
            // Text_MatKhauLayLai
            // 
            Text_MatKhauLayLai.Dock = DockStyle.Fill;
            Text_MatKhauLayLai.Location = new Point(234, 21);
            Text_MatKhauLayLai.Margin = new Padding(6, 0, 0, 0);
            Text_MatKhauLayLai.Name = "Text_MatKhauLayLai";
            Text_MatKhauLayLai.Size = new Size(222, 23);
            Text_MatKhauLayLai.TabIndex = 3;
            Text_MatKhauLayLai.TabStop = false;
            // 
            // pnlHero
            // 
            pnlHero.Controls.Add(tlpHero);
            pnlHero.Dock = DockStyle.Left;
            pnlHero.Location = new Point(0, 0);
            pnlHero.Name = "pnlHero";
            pnlHero.Size = new Size(300, 562);
            pnlHero.TabIndex = 1;
            // 
            // tlpHero
            // 
            tlpHero.BackColor = Color.Transparent;
            tlpHero.ColumnCount = 1;
            tlpHero.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpHero.Controls.Add(pnlBadge, 0, 0);
            tlpHero.Controls.Add(lblHeroTitle, 0, 1);
            tlpHero.Controls.Add(lblHeroDesc, 0, 2);
            tlpHero.Controls.Add(lblHeroHint, 0, 3);
            tlpHero.Dock = DockStyle.Fill;
            tlpHero.Location = new Point(0, 0);
            tlpHero.Name = "tlpHero";
            tlpHero.Padding = new Padding(24, 24, 24, 28);
            tlpHero.RowCount = 4;
            tlpHero.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tlpHero.RowStyles.Add(new RowStyle());
            tlpHero.RowStyles.Add(new RowStyle());
            tlpHero.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            tlpHero.Size = new Size(300, 562);
            tlpHero.TabIndex = 0;
            // 
            // pnlBadge
            // 
            pnlBadge.Anchor = AnchorStyles.Bottom;
            pnlBadge.BackColor = Color.White;
            pnlBadge.Controls.Add(PictureBox2);
            pnlBadge.CornerRadius = 70;
            pnlBadge.Cursor = Cursors.Hand;
            pnlBadge.Location = new Point(80, 48);
            pnlBadge.Margin = new Padding(0, 0, 0, 22);
            pnlBadge.Name = "pnlBadge";
            pnlBadge.Padding = new Padding(22);
            pnlBadge.Size = new Size(140, 140);
            pnlBadge.TabIndex = 0;
            pnlBadge.Click += PictureBox2_Click;
            // 
            // PictureBox2
            // 
            PictureBox2.BackColor = Color.White;
            PictureBox2.Cursor = Cursors.Hand;
            PictureBox2.Dock = DockStyle.Fill;
            PictureBox2.Location = new Point(22, 22);
            PictureBox2.Name = "PictureBox2";
            PictureBox2.Size = new Size(96, 96);
            PictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            PictureBox2.TabIndex = 0;
            PictureBox2.TabStop = false;
            PictureBox2.Click += PictureBox2_Click;
            // 
            // lblHeroTitle
            // 
            lblHeroTitle.Anchor = AnchorStyles.Top;
            lblHeroTitle.AutoSize = true;
            lblHeroTitle.BackColor = Color.Transparent;
            lblHeroTitle.Font = new Font("Segoe UI", 17F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblHeroTitle.ForeColor = Color.White;
            lblHeroTitle.Location = new Point(35, 210);
            lblHeroTitle.Margin = new Padding(0, 0, 0, 8);
            lblHeroTitle.Name = "lblHeroTitle";
            lblHeroTitle.Size = new Size(230, 31);
            lblHeroTitle.TabIndex = 1;
            lblHeroTitle.Text = "Khôi phục tài khoản";
            lblHeroTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblHeroDesc
            // 
            lblHeroDesc.Anchor = AnchorStyles.Top;
            lblHeroDesc.AutoSize = true;
            lblHeroDesc.BackColor = Color.Transparent;
            lblHeroDesc.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblHeroDesc.ForeColor = Color.FromArgb(224, 231, 255);
            lblHeroDesc.Location = new Point(28, 249);
            lblHeroDesc.Margin = new Padding(0);
            lblHeroDesc.MaximumSize = new Size(244, 0);
            lblHeroDesc.Name = "lblHeroDesc";
            lblHeroDesc.Size = new Size(243, 57);
            lblHeroDesc.TabIndex = 2;
            lblHeroDesc.Text = "Xác minh bằng câu hỏi bảo mật để lấy lại quyền truy cập một cách an toàn và nhanh chóng.";
            lblHeroDesc.TextAlign = ContentAlignment.TopCenter;
            // 
            // lblHeroHint
            // 
            lblHeroHint.Anchor = AnchorStyles.Bottom;
            lblHeroHint.AutoSize = true;
            lblHeroHint.BackColor = Color.Transparent;
            lblHeroHint.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblHeroHint.ForeColor = Color.FromArgb(199, 210, 254);
            lblHeroHint.Location = new Point(32, 504);
            lblHeroHint.Margin = new Padding(0);
            lblHeroHint.MaximumSize = new Size(244, 0);
            lblHeroHint.Name = "lblHeroHint";
            lblHeroHint.Size = new Size(235, 30);
            lblHeroHint.TabIndex = 3;
            lblHeroHint.Text = "Cần hỗ trợ? Nhấn vào biểu tượng phía trên để xem hướng dẫn.";
            lblHeroHint.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(0, 0);
            progressBar1.Margin = new Padding(0);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(544, 4);
            progressBar1.TabIndex = 1;
            progressBar1.Visible = false;
            // 
            // statusStrip1
            // 
            statusStrip1.BackColor = Color.FromArgb(238, 242, 249);
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { label1_PhienBanPhanMem, toolStripStatusLabel1 });
            statusStrip1.Location = new Point(0, 618);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(12, 0, 16, 0);
            statusStrip1.Size = new Size(900, 22);
            statusStrip1.TabIndex = 5;
            statusStrip1.Text = "statusStrip1";
            // 
            // label1_PhienBanPhanMem
            // 
            label1_PhienBanPhanMem.ForeColor = Color.FromArgb(100, 116, 139);
            label1_PhienBanPhanMem.Name = "label1_PhienBanPhanMem";
            label1_PhienBanPhanMem.Size = new Size(222, 17);
            label1_PhienBanPhanMem.Text = "Viết bởi LeTrungKien và Nhóm phát triển";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.ActiveLinkColor = Color.FromArgb(29, 78, 216);
            toolStripStatusLabel1.LinkColor = Color.FromArgb(37, 99, 235);
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(65, 17);
            toolStripStatusLabel1.Text = "HuongDan";
            // 
            // Form5_QuenPass
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(238, 242, 249);
            ClientSize = new Size(900, 640);
            Controls.Add(pnlRoot);
            Controls.Add(statusStrip1);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form5_QuenPass";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Quên mật khẩu";
            Load += Form5_Load;
            pnlRoot.ResumeLayout(false);
            pnlCard.ResumeLayout(false);
            pnlRight.ResumeLayout(false);
            tlpForm.ResumeLayout(false);
            tlpForm.PerformLayout();
            pnlCombo1.ResumeLayout(false);
            pnlCombo2.ResumeLayout(false);
            tlpKetQua.ResumeLayout(false);
            tlpKetQua.PerformLayout();
            pnlHero.ResumeLayout(false);
            tlpHero.ResumeLayout(false);
            tlpHero.PerformLayout();
            pnlBadge.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)PictureBox2).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion
        private BufferedPanel pnlRoot;
        private WebCardPanel pnlCard;
        private BufferedPanel pnlRight;
        private BufferedTableLayoutPanel tlpForm;
        private Label lblTieuDe;
        private Label lblMoTa;
        private Label lblCauHoi1;
        private Label lblCauHoi2;
        private WebFieldPanel pnlCombo1;
        private WebFieldPanel pnlCombo2;
        internal ComboBox ComboBox1_CauHoi1;
        internal ComboBox ComboBox2_CauHoi2;
        private Krypton.Toolkit.KryptonTextBox TextBox1_TraLoi1;
        private Krypton.Toolkit.KryptonTextBox TextBox2_TraLoi2;
        internal Krypton.Toolkit.KryptonButton Btn_KiemTra;
        private Label lblKetQua;
        private BufferedTableLayoutPanel tlpKetQua;
        private Label lblTaiKhoan;
        private Label lblMatKhau;
        private Krypton.Toolkit.KryptonTextBox Text_TenTaiKhoan;
        private Krypton.Toolkit.KryptonTextBox Text_MatKhauLayLai;
        private ProgressBar progressBar1;
        private HeroGradientPanel pnlHero;
        private BufferedTableLayoutPanel tlpHero;
        private WebCardPanel pnlBadge;
        internal PictureBox PictureBox2;
        private Label lblHeroTitle;
        private Label lblHeroDesc;
        private Label lblHeroHint;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel label1_PhienBanPhanMem;
        private ToolStripStatusLabel toolStripStatusLabel1;
    }
}