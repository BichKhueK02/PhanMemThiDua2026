namespace PhanMemThiDua2026
{
    partial class Form14
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

        // ============================================================
        //  DESIGN TOKENS (giao diện sáng kiểu web)
        //  Nền trang        #EEF2F9  (238, 242, 249)
        //  Banner / Primary #2563EB  (37, 99, 235)     hover #3B82F6  down #1D4ED8
        //  Vạch nhấn        #FACC15  (250, 204, 21)
        //  Thẻ / Phím số    #FFFFFF  viền #E2E8F0       hover #F1F5F9  down #E2E8F0
        //  Chữ chính        #1E293B  (30, 41, 59)
        //  Ngoặc / phẩy     nền #E0E7FF chữ #4338CA     hover #C7D2FE  down #A5B4FC
        //  Clear            nền #FEE2E2 chữ #B91C1C     hover #FECACA  down #FCA5A5
        //  Clear All        nền #FECACA chữ #991B1B     hover #FCA5A5  down #F87171
        //  Bằng (=)         nền #16A34A chữ trắng       hover #22C55E  down #15803D
        //
        //  LƯỚI: 4 cột x 6 hàng chia đều (Percent), mọi phím Dock.Fill, Margin 6.
        //  Mọi phím: TextAlign = MiddleCenter, Padding = 0, UseCompatibleTextRendering = true
        //  (căn giữa chữ chính xác, không rớt dòng / không bị cắt).
        // ============================================================

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form14));
            Btn_phimdong = new Button();
            Btn_phimmo = new Button();
            Btn_phimdauxoa = new Button();
            Btn_phimdauxoaall = new Button();
            Btn_phimdaubang = new Button();
            Btn_phimdauphay = new Button();
            Btn_sokhong = new Button();
            Btn_phimdauchia = new Button();
            Btn_phimdaunhan = new Button();
            Btn_phimdautru = new Button();
            Btn_phimdaucong = new Button();
            Btn_sochin = new Button();
            Btn_sotam = new Button();
            Btn_sonam = new Button();
            Btn_sosau = new Button();
            Btn_sobay = new Button();
            Btn_sobon = new Button();
            Btn_soba = new Button();
            Btn_sohai = new Button();
            Btn_somot = new Button();
            ListBox1 = new ListBox();
            tblMain = new TableLayoutPanel();
            tblHeader = new TableLayoutPanel();
            lblTieuDe = new Label();
            lblPhuDe = new Label();
            pnlHeader = new Panel();
            pnlStripe = new Panel();
            pnlDisplay = new Panel();
            tblPhim = new TableLayoutPanel();
            tblMain.SuspendLayout();
            tblHeader.SuspendLayout();
            pnlHeader.SuspendLayout();
            pnlDisplay.SuspendLayout();
            tblPhim.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader  (banner)
            // 
            pnlHeader.BackColor = Color.FromArgb(37, 99, 235);
            pnlHeader.Controls.Add(tblHeader);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Margin = new Padding(0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(440, 88);
            pnlHeader.TabIndex = 2;
            // 
            // tblHeader  (2 hàng: tiêu đề / phụ đề, tự căn theo kích thước)
            // 
            tblHeader.BackColor = Color.Transparent;
            tblHeader.ColumnCount = 1;
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblHeader.Controls.Add(lblTieuDe, 0, 0);
            tblHeader.Controls.Add(lblPhuDe, 0, 1);
            tblHeader.Dock = DockStyle.Fill;
            tblHeader.Location = new Point(0, 0);
            tblHeader.Margin = new Padding(0);
            tblHeader.Name = "tblHeader";
            tblHeader.Padding = new Padding(28, 8, 16, 10);
            tblHeader.RowCount = 2;
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
            tblHeader.Size = new Size(440, 88);
            tblHeader.TabIndex = 0;
            // 
            // lblTieuDe
            // 
            lblTieuDe.BackColor = Color.Transparent;
            lblTieuDe.Dock = DockStyle.Fill;
            lblTieuDe.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTieuDe.ForeColor = Color.White;
            lblTieuDe.Margin = new Padding(0);
            lblTieuDe.Name = "lblTieuDe";
            lblTieuDe.TabIndex = 0;
            lblTieuDe.Text = "Máy tính cơ bản";
            lblTieuDe.TextAlign = ContentAlignment.BottomLeft;
            // 
            // lblPhuDe
            // 
            lblPhuDe.BackColor = Color.Transparent;
            lblPhuDe.Dock = DockStyle.Fill;
            lblPhuDe.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblPhuDe.ForeColor = Color.FromArgb(219, 234, 254);
            lblPhuDe.Margin = new Padding(0);
            lblPhuDe.Name = "lblPhuDe";
            lblPhuDe.TabIndex = 1;
            lblPhuDe.Text = "Tính toán nhanh, gọn và rõ ràng";
            lblPhuDe.TextAlign = ContentAlignment.TopLeft;
            // 
            // pnlStripe  (vạch nhấn dưới banner)
            // 
            pnlStripe.BackColor = Color.FromArgb(250, 204, 21);
            pnlStripe.Dock = DockStyle.Top;
            pnlStripe.Location = new Point(0, 88);
            pnlStripe.Margin = new Padding(0);
            pnlStripe.Name = "pnlStripe";
            pnlStripe.Size = new Size(440, 4);
            pnlStripe.TabIndex = 1;
            // 
            // tblMain  (khu vực nội dung: màn hình + bàn phím)
            // 
            tblMain.BackColor = Color.FromArgb(238, 242, 249);
            tblMain.ColumnCount = 1;
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblMain.Controls.Add(pnlDisplay, 0, 0);
            tblMain.Controls.Add(tblPhim, 0, 1);
            tblMain.Dock = DockStyle.Fill;
            tblMain.Location = new Point(0, 0);
            tblMain.Margin = new Padding(0);
            tblMain.Name = "tblMain";
            tblMain.Padding = new Padding(22, 20, 22, 22);
            tblMain.RowCount = 2;
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblMain.Size = new Size(440, 660);
            tblMain.TabIndex = 0;
            // 
            // pnlDisplay  (thẻ màn hình)
            // 
            pnlDisplay.BackColor = Color.White;
            pnlDisplay.BorderStyle = BorderStyle.None;
            pnlDisplay.Controls.Add(ListBox1);
            pnlDisplay.Dock = DockStyle.Fill;
            pnlDisplay.Margin = new Padding(0, 0, 0, 16);
            pnlDisplay.Name = "pnlDisplay";
            pnlDisplay.Padding = new Padding(18, 12, 18, 12);
            pnlDisplay.TabIndex = 1;
            // 
            // ListBox1
            // 
            ListBox1.BackColor = Color.White;
            ListBox1.BorderStyle = BorderStyle.None;
            ListBox1.Dock = DockStyle.Fill;
            ListBox1.Font = new Font("Consolas", 22F, FontStyle.Bold, GraphicsUnit.Point, 0);
            ListBox1.ForeColor = Color.FromArgb(30, 41, 59);
            ListBox1.FormattingEnabled = true;
            ListBox1.IntegralHeight = false;
            ListBox1.ItemHeight = 36;
            ListBox1.Name = "ListBox1";
            ListBox1.TabIndex = 35;
            // 
            // tblPhim  (bàn phím 4 x 6, chia đều)
            // 
            tblPhim.BackColor = Color.Transparent;
            tblPhim.ColumnCount = 4;
            tblPhim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tblPhim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tblPhim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tblPhim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tblPhim.Controls.Add(Btn_phimmo, 0, 0);
            tblPhim.Controls.Add(Btn_phimdong, 1, 0);
            tblPhim.Controls.Add(Btn_phimdauxoa, 2, 0);
            tblPhim.Controls.Add(Btn_phimdauxoaall, 3, 0);
            tblPhim.Controls.Add(Btn_somot, 0, 1);
            tblPhim.Controls.Add(Btn_sohai, 1, 1);
            tblPhim.Controls.Add(Btn_soba, 2, 1);
            tblPhim.Controls.Add(Btn_phimdaucong, 3, 1);
            tblPhim.Controls.Add(Btn_sobon, 0, 2);
            tblPhim.Controls.Add(Btn_sonam, 1, 2);
            tblPhim.Controls.Add(Btn_sosau, 2, 2);
            tblPhim.Controls.Add(Btn_phimdautru, 3, 2);
            tblPhim.Controls.Add(Btn_sobay, 0, 3);
            tblPhim.Controls.Add(Btn_sotam, 1, 3);
            tblPhim.Controls.Add(Btn_sochin, 2, 3);
            tblPhim.Controls.Add(Btn_phimdaunhan, 3, 3);
            tblPhim.Controls.Add(Btn_sokhong, 0, 4);
            tblPhim.Controls.Add(Btn_phimdauphay, 2, 4);
            tblPhim.Controls.Add(Btn_phimdauchia, 3, 4);
            tblPhim.Controls.Add(Btn_phimdaubang, 0, 5);
            tblPhim.Dock = DockStyle.Fill;
            tblPhim.Margin = new Padding(0);
            tblPhim.Name = "tblPhim";
            tblPhim.RowCount = 6;
            tblPhim.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666F));
            tblPhim.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666F));
            tblPhim.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666F));
            tblPhim.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666F));
            tblPhim.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666F));
            tblPhim.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666F));
            tblPhim.TabIndex = 2;
            tblPhim.SetColumnSpan(Btn_sokhong, 2);
            tblPhim.SetColumnSpan(Btn_phimdaubang, 4);
            // 
            // Btn_phimmo  "("
            // 
            Btn_phimmo.AutoEllipsis = false;
            Btn_phimmo.BackColor = Color.FromArgb(224, 231, 255);
            Btn_phimmo.Cursor = Cursors.Hand;
            Btn_phimmo.Dock = DockStyle.Fill;
            Btn_phimmo.FlatAppearance.BorderSize = 0;
            Btn_phimmo.FlatAppearance.MouseDownBackColor = Color.FromArgb(165, 180, 252);
            Btn_phimmo.FlatAppearance.MouseOverBackColor = Color.FromArgb(199, 210, 254);
            Btn_phimmo.FlatStyle = FlatStyle.Flat;
            Btn_phimmo.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_phimmo.ForeColor = Color.FromArgb(67, 56, 202);
            Btn_phimmo.Margin = new Padding(6);
            Btn_phimmo.Name = "Btn_phimmo";
            Btn_phimmo.Padding = new Padding(0);
            Btn_phimmo.TabIndex = 1;
            Btn_phimmo.Text = "(";
            Btn_phimmo.TextAlign = ContentAlignment.MiddleCenter;
            Btn_phimmo.UseCompatibleTextRendering = true;
            Btn_phimmo.UseMnemonic = false;
            Btn_phimmo.UseVisualStyleBackColor = false;
            // 
            // Btn_phimdong  ")"
            // 
            Btn_phimdong.AutoEllipsis = false;
            Btn_phimdong.BackColor = Color.FromArgb(224, 231, 255);
            Btn_phimdong.Cursor = Cursors.Hand;
            Btn_phimdong.Dock = DockStyle.Fill;
            Btn_phimdong.FlatAppearance.BorderSize = 0;
            Btn_phimdong.FlatAppearance.MouseDownBackColor = Color.FromArgb(165, 180, 252);
            Btn_phimdong.FlatAppearance.MouseOverBackColor = Color.FromArgb(199, 210, 254);
            Btn_phimdong.FlatStyle = FlatStyle.Flat;
            Btn_phimdong.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_phimdong.ForeColor = Color.FromArgb(67, 56, 202);
            Btn_phimdong.Margin = new Padding(6);
            Btn_phimdong.Name = "Btn_phimdong";
            Btn_phimdong.Padding = new Padding(0);
            Btn_phimdong.TabIndex = 2;
            Btn_phimdong.Text = ")";
            Btn_phimdong.TextAlign = ContentAlignment.MiddleCenter;
            Btn_phimdong.UseCompatibleTextRendering = true;
            Btn_phimdong.UseMnemonic = false;
            Btn_phimdong.UseVisualStyleBackColor = false;
            // 
            // Btn_phimdauxoa  "Clear"
            // 
            Btn_phimdauxoa.AutoEllipsis = false;
            Btn_phimdauxoa.BackColor = Color.FromArgb(254, 226, 226);
            Btn_phimdauxoa.Cursor = Cursors.Hand;
            Btn_phimdauxoa.Dock = DockStyle.Fill;
            Btn_phimdauxoa.FlatAppearance.BorderSize = 0;
            Btn_phimdauxoa.FlatAppearance.MouseDownBackColor = Color.FromArgb(252, 165, 165);
            Btn_phimdauxoa.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 202, 202);
            Btn_phimdauxoa.FlatStyle = FlatStyle.Flat;
            Btn_phimdauxoa.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_phimdauxoa.ForeColor = Color.FromArgb(185, 28, 28);
            Btn_phimdauxoa.Margin = new Padding(6);
            Btn_phimdauxoa.Name = "Btn_phimdauxoa";
            Btn_phimdauxoa.Padding = new Padding(0);
            Btn_phimdauxoa.TabIndex = 3;
            Btn_phimdauxoa.Text = "Clear";
            Btn_phimdauxoa.TextAlign = ContentAlignment.MiddleCenter;
            Btn_phimdauxoa.UseCompatibleTextRendering = true;
            Btn_phimdauxoa.UseMnemonic = false;
            Btn_phimdauxoa.UseVisualStyleBackColor = false;
            // 
            // Btn_phimdauxoaall  "Clear All"
            // 
            Btn_phimdauxoaall.AutoEllipsis = false;
            Btn_phimdauxoaall.BackColor = Color.FromArgb(254, 202, 202);
            Btn_phimdauxoaall.Cursor = Cursors.Hand;
            Btn_phimdauxoaall.Dock = DockStyle.Fill;
            Btn_phimdauxoaall.FlatAppearance.BorderSize = 0;
            Btn_phimdauxoaall.FlatAppearance.MouseDownBackColor = Color.FromArgb(248, 113, 113);
            Btn_phimdauxoaall.FlatAppearance.MouseOverBackColor = Color.FromArgb(252, 165, 165);
            Btn_phimdauxoaall.FlatStyle = FlatStyle.Flat;
            Btn_phimdauxoaall.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_phimdauxoaall.ForeColor = Color.FromArgb(153, 27, 27);
            Btn_phimdauxoaall.Margin = new Padding(6);
            Btn_phimdauxoaall.Name = "Btn_phimdauxoaall";
            Btn_phimdauxoaall.Padding = new Padding(0);
            Btn_phimdauxoaall.TabIndex = 4;
            Btn_phimdauxoaall.Text = "Clear All";
            Btn_phimdauxoaall.TextAlign = ContentAlignment.MiddleCenter;
            Btn_phimdauxoaall.UseCompatibleTextRendering = true;
            Btn_phimdauxoaall.UseMnemonic = false;
            Btn_phimdauxoaall.UseVisualStyleBackColor = false;
            // 
            // Btn_somot  "1"
            // 
            Btn_somot.AutoEllipsis = false;
            Btn_somot.BackColor = Color.White;
            Btn_somot.Cursor = Cursors.Hand;
            Btn_somot.Dock = DockStyle.Fill;
            Btn_somot.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            Btn_somot.FlatAppearance.BorderSize = 1;
            Btn_somot.FlatAppearance.MouseDownBackColor = Color.FromArgb(226, 232, 240);
            Btn_somot.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            Btn_somot.FlatStyle = FlatStyle.Flat;
            Btn_somot.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_somot.ForeColor = Color.FromArgb(30, 41, 59);
            Btn_somot.Margin = new Padding(6);
            Btn_somot.Name = "Btn_somot";
            Btn_somot.Padding = new Padding(0);
            Btn_somot.TabIndex = 5;
            Btn_somot.Text = "1";
            Btn_somot.TextAlign = ContentAlignment.MiddleCenter;
            Btn_somot.UseCompatibleTextRendering = true;
            Btn_somot.UseMnemonic = false;
            Btn_somot.UseVisualStyleBackColor = false;
            // 
            // Btn_sohai  "2"
            // 
            Btn_sohai.AutoEllipsis = false;
            Btn_sohai.BackColor = Color.White;
            Btn_sohai.Cursor = Cursors.Hand;
            Btn_sohai.Dock = DockStyle.Fill;
            Btn_sohai.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            Btn_sohai.FlatAppearance.BorderSize = 1;
            Btn_sohai.FlatAppearance.MouseDownBackColor = Color.FromArgb(226, 232, 240);
            Btn_sohai.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            Btn_sohai.FlatStyle = FlatStyle.Flat;
            Btn_sohai.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_sohai.ForeColor = Color.FromArgb(30, 41, 59);
            Btn_sohai.Margin = new Padding(6);
            Btn_sohai.Name = "Btn_sohai";
            Btn_sohai.Padding = new Padding(0);
            Btn_sohai.TabIndex = 6;
            Btn_sohai.Text = "2";
            Btn_sohai.TextAlign = ContentAlignment.MiddleCenter;
            Btn_sohai.UseCompatibleTextRendering = true;
            Btn_sohai.UseMnemonic = false;
            Btn_sohai.UseVisualStyleBackColor = false;
            // 
            // Btn_soba  "3"
            // 
            Btn_soba.AutoEllipsis = false;
            Btn_soba.BackColor = Color.White;
            Btn_soba.Cursor = Cursors.Hand;
            Btn_soba.Dock = DockStyle.Fill;
            Btn_soba.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            Btn_soba.FlatAppearance.BorderSize = 1;
            Btn_soba.FlatAppearance.MouseDownBackColor = Color.FromArgb(226, 232, 240);
            Btn_soba.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            Btn_soba.FlatStyle = FlatStyle.Flat;
            Btn_soba.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_soba.ForeColor = Color.FromArgb(30, 41, 59);
            Btn_soba.Margin = new Padding(6);
            Btn_soba.Name = "Btn_soba";
            Btn_soba.Padding = new Padding(0);
            Btn_soba.TabIndex = 7;
            Btn_soba.Text = "3";
            Btn_soba.TextAlign = ContentAlignment.MiddleCenter;
            Btn_soba.UseCompatibleTextRendering = true;
            Btn_soba.UseMnemonic = false;
            Btn_soba.UseVisualStyleBackColor = false;
            // 
            // Btn_phimdaucong  "+"
            // 
            Btn_phimdaucong.AutoEllipsis = false;
            Btn_phimdaucong.BackColor = Color.FromArgb(37, 99, 235);
            Btn_phimdaucong.Cursor = Cursors.Hand;
            Btn_phimdaucong.Dock = DockStyle.Fill;
            Btn_phimdaucong.FlatAppearance.BorderSize = 0;
            Btn_phimdaucong.FlatAppearance.MouseDownBackColor = Color.FromArgb(29, 78, 216);
            Btn_phimdaucong.FlatAppearance.MouseOverBackColor = Color.FromArgb(59, 130, 246);
            Btn_phimdaucong.FlatStyle = FlatStyle.Flat;
            Btn_phimdaucong.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_phimdaucong.ForeColor = Color.White;
            Btn_phimdaucong.Margin = new Padding(6);
            Btn_phimdaucong.Name = "Btn_phimdaucong";
            Btn_phimdaucong.Padding = new Padding(0);
            Btn_phimdaucong.TabIndex = 8;
            Btn_phimdaucong.Text = "+";
            Btn_phimdaucong.TextAlign = ContentAlignment.MiddleCenter;
            Btn_phimdaucong.UseCompatibleTextRendering = true;
            Btn_phimdaucong.UseMnemonic = false;
            Btn_phimdaucong.UseVisualStyleBackColor = false;
            // 
            // Btn_sobon  "4"
            // 
            Btn_sobon.AutoEllipsis = false;
            Btn_sobon.BackColor = Color.White;
            Btn_sobon.Cursor = Cursors.Hand;
            Btn_sobon.Dock = DockStyle.Fill;
            Btn_sobon.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            Btn_sobon.FlatAppearance.BorderSize = 1;
            Btn_sobon.FlatAppearance.MouseDownBackColor = Color.FromArgb(226, 232, 240);
            Btn_sobon.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            Btn_sobon.FlatStyle = FlatStyle.Flat;
            Btn_sobon.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_sobon.ForeColor = Color.FromArgb(30, 41, 59);
            Btn_sobon.Margin = new Padding(6);
            Btn_sobon.Name = "Btn_sobon";
            Btn_sobon.Padding = new Padding(0);
            Btn_sobon.TabIndex = 9;
            Btn_sobon.Text = "4";
            Btn_sobon.TextAlign = ContentAlignment.MiddleCenter;
            Btn_sobon.UseCompatibleTextRendering = true;
            Btn_sobon.UseMnemonic = false;
            Btn_sobon.UseVisualStyleBackColor = false;
            // 
            // Btn_sonam  "5"
            // 
            Btn_sonam.AutoEllipsis = false;
            Btn_sonam.BackColor = Color.White;
            Btn_sonam.Cursor = Cursors.Hand;
            Btn_sonam.Dock = DockStyle.Fill;
            Btn_sonam.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            Btn_sonam.FlatAppearance.BorderSize = 1;
            Btn_sonam.FlatAppearance.MouseDownBackColor = Color.FromArgb(226, 232, 240);
            Btn_sonam.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            Btn_sonam.FlatStyle = FlatStyle.Flat;
            Btn_sonam.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_sonam.ForeColor = Color.FromArgb(30, 41, 59);
            Btn_sonam.Margin = new Padding(6);
            Btn_sonam.Name = "Btn_sonam";
            Btn_sonam.Padding = new Padding(0);
            Btn_sonam.TabIndex = 10;
            Btn_sonam.Text = "5";
            Btn_sonam.TextAlign = ContentAlignment.MiddleCenter;
            Btn_sonam.UseCompatibleTextRendering = true;
            Btn_sonam.UseMnemonic = false;
            Btn_sonam.UseVisualStyleBackColor = false;
            // 
            // Btn_sosau  "6"
            // 
            Btn_sosau.AutoEllipsis = false;
            Btn_sosau.BackColor = Color.White;
            Btn_sosau.Cursor = Cursors.Hand;
            Btn_sosau.Dock = DockStyle.Fill;
            Btn_sosau.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            Btn_sosau.FlatAppearance.BorderSize = 1;
            Btn_sosau.FlatAppearance.MouseDownBackColor = Color.FromArgb(226, 232, 240);
            Btn_sosau.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            Btn_sosau.FlatStyle = FlatStyle.Flat;
            Btn_sosau.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_sosau.ForeColor = Color.FromArgb(30, 41, 59);
            Btn_sosau.Margin = new Padding(6);
            Btn_sosau.Name = "Btn_sosau";
            Btn_sosau.Padding = new Padding(0);
            Btn_sosau.TabIndex = 11;
            Btn_sosau.Text = "6";
            Btn_sosau.TextAlign = ContentAlignment.MiddleCenter;
            Btn_sosau.UseCompatibleTextRendering = true;
            Btn_sosau.UseMnemonic = false;
            Btn_sosau.UseVisualStyleBackColor = false;
            // 
            // Btn_phimdautru  "-"
            // 
            Btn_phimdautru.AutoEllipsis = false;
            Btn_phimdautru.BackColor = Color.FromArgb(37, 99, 235);
            Btn_phimdautru.Cursor = Cursors.Hand;
            Btn_phimdautru.Dock = DockStyle.Fill;
            Btn_phimdautru.FlatAppearance.BorderSize = 0;
            Btn_phimdautru.FlatAppearance.MouseDownBackColor = Color.FromArgb(29, 78, 216);
            Btn_phimdautru.FlatAppearance.MouseOverBackColor = Color.FromArgb(59, 130, 246);
            Btn_phimdautru.FlatStyle = FlatStyle.Flat;
            Btn_phimdautru.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_phimdautru.ForeColor = Color.White;
            Btn_phimdautru.Margin = new Padding(6);
            Btn_phimdautru.Name = "Btn_phimdautru";
            Btn_phimdautru.Padding = new Padding(0);
            Btn_phimdautru.TabIndex = 12;
            Btn_phimdautru.Text = "-";
            Btn_phimdautru.TextAlign = ContentAlignment.MiddleCenter;
            Btn_phimdautru.UseCompatibleTextRendering = true;
            Btn_phimdautru.UseMnemonic = false;
            Btn_phimdautru.UseVisualStyleBackColor = false;
            // 
            // Btn_sobay  "7"
            // 
            Btn_sobay.AutoEllipsis = false;
            Btn_sobay.BackColor = Color.White;
            Btn_sobay.Cursor = Cursors.Hand;
            Btn_sobay.Dock = DockStyle.Fill;
            Btn_sobay.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            Btn_sobay.FlatAppearance.BorderSize = 1;
            Btn_sobay.FlatAppearance.MouseDownBackColor = Color.FromArgb(226, 232, 240);
            Btn_sobay.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            Btn_sobay.FlatStyle = FlatStyle.Flat;
            Btn_sobay.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_sobay.ForeColor = Color.FromArgb(30, 41, 59);
            Btn_sobay.Margin = new Padding(6);
            Btn_sobay.Name = "Btn_sobay";
            Btn_sobay.Padding = new Padding(0);
            Btn_sobay.TabIndex = 13;
            Btn_sobay.Text = "7";
            Btn_sobay.TextAlign = ContentAlignment.MiddleCenter;
            Btn_sobay.UseCompatibleTextRendering = true;
            Btn_sobay.UseMnemonic = false;
            Btn_sobay.UseVisualStyleBackColor = false;
            // 
            // Btn_sotam  "8"
            // 
            Btn_sotam.AutoEllipsis = false;
            Btn_sotam.BackColor = Color.White;
            Btn_sotam.Cursor = Cursors.Hand;
            Btn_sotam.Dock = DockStyle.Fill;
            Btn_sotam.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            Btn_sotam.FlatAppearance.BorderSize = 1;
            Btn_sotam.FlatAppearance.MouseDownBackColor = Color.FromArgb(226, 232, 240);
            Btn_sotam.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            Btn_sotam.FlatStyle = FlatStyle.Flat;
            Btn_sotam.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_sotam.ForeColor = Color.FromArgb(30, 41, 59);
            Btn_sotam.Margin = new Padding(6);
            Btn_sotam.Name = "Btn_sotam";
            Btn_sotam.Padding = new Padding(0);
            Btn_sotam.TabIndex = 14;
            Btn_sotam.Text = "8";
            Btn_sotam.TextAlign = ContentAlignment.MiddleCenter;
            Btn_sotam.UseCompatibleTextRendering = true;
            Btn_sotam.UseMnemonic = false;
            Btn_sotam.UseVisualStyleBackColor = false;
            // 
            // Btn_sochin  "9"
            // 
            Btn_sochin.AutoEllipsis = false;
            Btn_sochin.BackColor = Color.White;
            Btn_sochin.Cursor = Cursors.Hand;
            Btn_sochin.Dock = DockStyle.Fill;
            Btn_sochin.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            Btn_sochin.FlatAppearance.BorderSize = 1;
            Btn_sochin.FlatAppearance.MouseDownBackColor = Color.FromArgb(226, 232, 240);
            Btn_sochin.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            Btn_sochin.FlatStyle = FlatStyle.Flat;
            Btn_sochin.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_sochin.ForeColor = Color.FromArgb(30, 41, 59);
            Btn_sochin.Margin = new Padding(6);
            Btn_sochin.Name = "Btn_sochin";
            Btn_sochin.Padding = new Padding(0);
            Btn_sochin.TabIndex = 15;
            Btn_sochin.Text = "9";
            Btn_sochin.TextAlign = ContentAlignment.MiddleCenter;
            Btn_sochin.UseCompatibleTextRendering = true;
            Btn_sochin.UseMnemonic = false;
            Btn_sochin.UseVisualStyleBackColor = false;
            // 
            // Btn_phimdaunhan  "x"
            // 
            Btn_phimdaunhan.AutoEllipsis = false;
            Btn_phimdaunhan.BackColor = Color.FromArgb(37, 99, 235);
            Btn_phimdaunhan.Cursor = Cursors.Hand;
            Btn_phimdaunhan.Dock = DockStyle.Fill;
            Btn_phimdaunhan.FlatAppearance.BorderSize = 0;
            Btn_phimdaunhan.FlatAppearance.MouseDownBackColor = Color.FromArgb(29, 78, 216);
            Btn_phimdaunhan.FlatAppearance.MouseOverBackColor = Color.FromArgb(59, 130, 246);
            Btn_phimdaunhan.FlatStyle = FlatStyle.Flat;
            Btn_phimdaunhan.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_phimdaunhan.ForeColor = Color.White;
            Btn_phimdaunhan.Margin = new Padding(6);
            Btn_phimdaunhan.Name = "Btn_phimdaunhan";
            Btn_phimdaunhan.Padding = new Padding(0);
            Btn_phimdaunhan.TabIndex = 16;
            Btn_phimdaunhan.Text = "x";
            Btn_phimdaunhan.TextAlign = ContentAlignment.MiddleCenter;
            Btn_phimdaunhan.UseCompatibleTextRendering = true;
            Btn_phimdaunhan.UseMnemonic = false;
            Btn_phimdaunhan.UseVisualStyleBackColor = false;
            // 
            // Btn_sokhong  "0"  (chiếm 2 cột)
            // 
            Btn_sokhong.AutoEllipsis = false;
            Btn_sokhong.BackColor = Color.White;
            Btn_sokhong.Cursor = Cursors.Hand;
            Btn_sokhong.Dock = DockStyle.Fill;
            Btn_sokhong.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            Btn_sokhong.FlatAppearance.BorderSize = 1;
            Btn_sokhong.FlatAppearance.MouseDownBackColor = Color.FromArgb(226, 232, 240);
            Btn_sokhong.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            Btn_sokhong.FlatStyle = FlatStyle.Flat;
            Btn_sokhong.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_sokhong.ForeColor = Color.FromArgb(30, 41, 59);
            Btn_sokhong.Margin = new Padding(6);
            Btn_sokhong.Name = "Btn_sokhong";
            Btn_sokhong.Padding = new Padding(0);
            Btn_sokhong.TabIndex = 17;
            Btn_sokhong.Text = "0";
            Btn_sokhong.TextAlign = ContentAlignment.MiddleCenter;
            Btn_sokhong.UseCompatibleTextRendering = true;
            Btn_sokhong.UseMnemonic = false;
            Btn_sokhong.UseVisualStyleBackColor = false;
            // 
            // Btn_phimdauphay  ","
            // 
            Btn_phimdauphay.AutoEllipsis = false;
            Btn_phimdauphay.BackColor = Color.FromArgb(224, 231, 255);
            Btn_phimdauphay.Cursor = Cursors.Hand;
            Btn_phimdauphay.Dock = DockStyle.Fill;
            Btn_phimdauphay.FlatAppearance.BorderSize = 0;
            Btn_phimdauphay.FlatAppearance.MouseDownBackColor = Color.FromArgb(165, 180, 252);
            Btn_phimdauphay.FlatAppearance.MouseOverBackColor = Color.FromArgb(199, 210, 254);
            Btn_phimdauphay.FlatStyle = FlatStyle.Flat;
            Btn_phimdauphay.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_phimdauphay.ForeColor = Color.FromArgb(67, 56, 202);
            Btn_phimdauphay.Margin = new Padding(6);
            Btn_phimdauphay.Name = "Btn_phimdauphay";
            Btn_phimdauphay.Padding = new Padding(0);
            Btn_phimdauphay.TabIndex = 18;
            Btn_phimdauphay.Text = ",";
            Btn_phimdauphay.TextAlign = ContentAlignment.MiddleCenter;
            Btn_phimdauphay.UseCompatibleTextRendering = true;
            Btn_phimdauphay.UseMnemonic = false;
            Btn_phimdauphay.UseVisualStyleBackColor = false;
            // 
            // Btn_phimdauchia  "/"
            // 
            Btn_phimdauchia.AutoEllipsis = false;
            Btn_phimdauchia.BackColor = Color.FromArgb(37, 99, 235);
            Btn_phimdauchia.Cursor = Cursors.Hand;
            Btn_phimdauchia.Dock = DockStyle.Fill;
            Btn_phimdauchia.FlatAppearance.BorderSize = 0;
            Btn_phimdauchia.FlatAppearance.MouseDownBackColor = Color.FromArgb(29, 78, 216);
            Btn_phimdauchia.FlatAppearance.MouseOverBackColor = Color.FromArgb(59, 130, 246);
            Btn_phimdauchia.FlatStyle = FlatStyle.Flat;
            Btn_phimdauchia.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_phimdauchia.ForeColor = Color.White;
            Btn_phimdauchia.Margin = new Padding(6);
            Btn_phimdauchia.Name = "Btn_phimdauchia";
            Btn_phimdauchia.Padding = new Padding(0);
            Btn_phimdauchia.TabIndex = 19;
            Btn_phimdauchia.Text = "/";
            Btn_phimdauchia.TextAlign = ContentAlignment.MiddleCenter;
            Btn_phimdauchia.UseCompatibleTextRendering = true;
            Btn_phimdauchia.UseMnemonic = false;
            Btn_phimdauchia.UseVisualStyleBackColor = false;
            // 
            // Btn_phimdaubang  "="  (chiếm 4 cột)
            // 
            Btn_phimdaubang.AutoEllipsis = false;
            Btn_phimdaubang.BackColor = Color.FromArgb(22, 163, 74);
            Btn_phimdaubang.Cursor = Cursors.Hand;
            Btn_phimdaubang.Dock = DockStyle.Fill;
            Btn_phimdaubang.FlatAppearance.BorderSize = 0;
            Btn_phimdaubang.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 128, 61);
            Btn_phimdaubang.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 197, 94);
            Btn_phimdaubang.FlatStyle = FlatStyle.Flat;
            Btn_phimdaubang.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_phimdaubang.ForeColor = Color.White;
            Btn_phimdaubang.Margin = new Padding(6);
            Btn_phimdaubang.Name = "Btn_phimdaubang";
            Btn_phimdaubang.Padding = new Padding(0);
            Btn_phimdaubang.TabIndex = 20;
            Btn_phimdaubang.Text = "=";
            Btn_phimdaubang.TextAlign = ContentAlignment.MiddleCenter;
            Btn_phimdaubang.UseCompatibleTextRendering = true;
            Btn_phimdaubang.UseMnemonic = false;
            Btn_phimdaubang.UseVisualStyleBackColor = false;
            // 
            // Form14
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(238, 242, 249);
            ClientSize = new Size(440, 660);
            Controls.Add(tblMain);
            Controls.Add(pnlStripe);
            Controls.Add(pnlHeader);
            DoubleBuffered = true;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(2);
            MinimumSize = new Size(380, 580);
            Name = "Form14";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Máy tính cơ bản";
            tblMain.ResumeLayout(false);
            tblHeader.ResumeLayout(false);
            pnlHeader.ResumeLayout(false);
            pnlDisplay.ResumeLayout(false);
            tblPhim.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tblMain;
        private TableLayoutPanel tblPhim;
        private TableLayoutPanel tblHeader;
        private Panel pnlHeader;
        private Panel pnlStripe;
        private Panel pnlDisplay;
        private Label lblTieuDe;
        private Label lblPhuDe;

        internal Button Btn_phimdong;
        internal Button Btn_phimmo;
        internal Button Btn_phimdauxoa;
        internal Button Btn_phimdauxoaall;
        internal Button Btn_phimdaubang;
        internal Button Btn_phimdauphay;
        internal Button Btn_sokhong;
        internal Button Btn_phimdauchia;
        internal Button Btn_phimdaunhan;
        internal Button Btn_phimdautru;
        internal Button Btn_phimdaucong;
        internal Button Btn_sochin;
        internal Button Btn_sotam;
        internal Button Btn_sonam;
        internal Button Btn_sosau;
        internal Button Btn_sobay;
        internal Button Btn_sobon;
        internal Button Btn_soba;
        internal Button Btn_sohai;
        internal Button Btn_somot;
        internal ListBox ListBox1;
    }
}