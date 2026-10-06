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

        // Giao diện tối hiện đại (không dùng trắng):
        // Nền #181825 | Màn hình #11111B chữ xanh lá #A6E3A1
        // Số: nền #313244 chữ #BAC2DE | Ngoặc, phẩy: nền #45475A chữ tím #CBA6F7
        // Phép toán: cam đào #FAB387 | Clear: hồng #F38BA8 | Clear All: hồng nhạt #EBA0AC | "=": xanh lá #A6E3A1
        // Phím màu dùng chữ tối #1E1E2E để luôn rõ nét.

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
            lblTieuDe = new Label();
            pnlStripe = new Panel();
            pnlDisplay = new Panel();
            tblPhim = new TableLayoutPanel();
            tblMain.SuspendLayout();
            pnlDisplay.SuspendLayout();
            tblPhim.SuspendLayout();
            SuspendLayout();
            // 
            // pnlStripe
            // 
            pnlStripe.BackColor = Color.FromArgb(137, 180, 250);
            pnlStripe.Dock = DockStyle.Top;
            pnlStripe.Location = new Point(0, 0);
            pnlStripe.Name = "pnlStripe";
            pnlStripe.Size = new Size(380, 6);
            pnlStripe.TabIndex = 1;
            // 
            // tblMain
            // 
            tblMain.BackColor = Color.FromArgb(24, 24, 37);
            tblMain.ColumnCount = 1;
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblMain.Controls.Add(lblTieuDe, 0, 0);
            tblMain.Controls.Add(pnlDisplay, 0, 1);
            tblMain.Controls.Add(tblPhim, 0, 2);
            tblMain.Dock = DockStyle.Fill;
            tblMain.Location = new Point(0, 0);
            tblMain.Name = "tblMain";
            tblMain.Padding = new Padding(16, 8, 16, 16);
            tblMain.RowCount = 3;
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 130F));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblMain.Size = new Size(380, 580);
            tblMain.TabIndex = 0;
            // 
            // lblTieuDe
            // 
            lblTieuDe.BackColor = Color.Transparent;
            lblTieuDe.Dock = DockStyle.Fill;
            lblTieuDe.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblTieuDe.ForeColor = Color.FromArgb(137, 180, 250);
            lblTieuDe.Location = new Point(19, 8);
            lblTieuDe.Name = "lblTieuDe";
            lblTieuDe.Size = new Size(342, 40);
            lblTieuDe.TabIndex = 0;
            lblTieuDe.Text = "Máy tính cơ bản";
            lblTieuDe.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pnlDisplay
            // 
            pnlDisplay.BackColor = Color.FromArgb(17, 17, 27);
            pnlDisplay.BorderStyle = BorderStyle.FixedSingle;
            pnlDisplay.Controls.Add(ListBox1);
            pnlDisplay.Dock = DockStyle.Fill;
            pnlDisplay.Location = new Point(19, 51);
            pnlDisplay.Margin = new Padding(3, 3, 3, 12);
            pnlDisplay.Name = "pnlDisplay";
            pnlDisplay.Padding = new Padding(10, 8, 10, 8);
            pnlDisplay.Size = new Size(342, 124);
            pnlDisplay.TabIndex = 1;
            // 
            // ListBox1
            // 
            ListBox1.BackColor = Color.FromArgb(17, 17, 27);
            ListBox1.BorderStyle = BorderStyle.None;
            ListBox1.Dock = DockStyle.Fill;
            ListBox1.Font = new Font("Consolas", 24F, FontStyle.Bold);
            ListBox1.ForeColor = Color.FromArgb(166, 227, 161);
            ListBox1.FormattingEnabled = true;
            ListBox1.IntegralHeight = false;
            ListBox1.ItemHeight = 38;
            ListBox1.Location = new Point(10, 8);
            ListBox1.Name = "ListBox1";
            ListBox1.Size = new Size(320, 106);
            ListBox1.TabIndex = 35;
            // 
            // tblPhim
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
            tblPhim.Location = new Point(19, 190);
            tblPhim.Name = "tblPhim";
            tblPhim.RowCount = 6;
            tblPhim.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6667F));
            tblPhim.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6667F));
            tblPhim.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6667F));
            tblPhim.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6667F));
            tblPhim.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6667F));
            tblPhim.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6667F));
            tblPhim.Size = new Size(342, 374);
            tblPhim.TabIndex = 2;
            tblPhim.SetColumnSpan(Btn_sokhong, 2);
            tblPhim.SetColumnSpan(Btn_phimdaubang, 4);
            // 
            // Btn_phimmo  "("
            // 
            Btn_phimmo.BackColor = Color.FromArgb(69, 71, 90);
            Btn_phimmo.Cursor = Cursors.Hand;
            Btn_phimmo.Dock = DockStyle.Fill;
            Btn_phimmo.FlatAppearance.BorderSize = 0;
            Btn_phimmo.FlatAppearance.MouseDownBackColor = Color.FromArgb(108, 112, 134);
            Btn_phimmo.FlatAppearance.MouseOverBackColor = Color.FromArgb(88, 91, 112);
            Btn_phimmo.FlatStyle = FlatStyle.Flat;
            Btn_phimmo.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            Btn_phimmo.ForeColor = Color.FromArgb(203, 166, 247);
            Btn_phimmo.Margin = new Padding(4);
            Btn_phimmo.Name = "Btn_phimmo";
            Btn_phimmo.TabIndex = 1;
            Btn_phimmo.Text = "(";
            Btn_phimmo.UseVisualStyleBackColor = false;
            // 
            // Btn_phimdong  ")"
            // 
            Btn_phimdong.BackColor = Color.FromArgb(69, 71, 90);
            Btn_phimdong.Cursor = Cursors.Hand;
            Btn_phimdong.Dock = DockStyle.Fill;
            Btn_phimdong.FlatAppearance.BorderSize = 0;
            Btn_phimdong.FlatAppearance.MouseDownBackColor = Color.FromArgb(108, 112, 134);
            Btn_phimdong.FlatAppearance.MouseOverBackColor = Color.FromArgb(88, 91, 112);
            Btn_phimdong.FlatStyle = FlatStyle.Flat;
            Btn_phimdong.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            Btn_phimdong.ForeColor = Color.FromArgb(203, 166, 247);
            Btn_phimdong.Margin = new Padding(4);
            Btn_phimdong.Name = "Btn_phimdong";
            Btn_phimdong.TabIndex = 2;
            Btn_phimdong.Text = ")";
            Btn_phimdong.UseVisualStyleBackColor = false;
            // 
            // Btn_phimdauxoa  "Clear"
            // 
            Btn_phimdauxoa.BackColor = Color.FromArgb(243, 139, 168);
            Btn_phimdauxoa.Cursor = Cursors.Hand;
            Btn_phimdauxoa.Dock = DockStyle.Fill;
            Btn_phimdauxoa.FlatAppearance.BorderSize = 0;
            Btn_phimdauxoa.FlatAppearance.MouseDownBackColor = Color.FromArgb(235, 115, 150);
            Btn_phimdauxoa.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 160, 184);
            Btn_phimdauxoa.FlatStyle = FlatStyle.Flat;
            Btn_phimdauxoa.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            Btn_phimdauxoa.ForeColor = Color.FromArgb(30, 30, 46);
            Btn_phimdauxoa.Margin = new Padding(4);
            Btn_phimdauxoa.Name = "Btn_phimdauxoa";
            Btn_phimdauxoa.TabIndex = 3;
            Btn_phimdauxoa.Text = "Clear";
            Btn_phimdauxoa.UseVisualStyleBackColor = false;
            // 
            // Btn_phimdauxoaall  "Clear All"
            // 
            Btn_phimdauxoaall.BackColor = Color.FromArgb(235, 160, 172);
            Btn_phimdauxoaall.Cursor = Cursors.Hand;
            Btn_phimdauxoaall.Dock = DockStyle.Fill;
            Btn_phimdauxoaall.FlatAppearance.BorderSize = 0;
            Btn_phimdauxoaall.FlatAppearance.MouseDownBackColor = Color.FromArgb(225, 140, 155);
            Btn_phimdauxoaall.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 178, 188);
            Btn_phimdauxoaall.FlatStyle = FlatStyle.Flat;
            Btn_phimdauxoaall.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            Btn_phimdauxoaall.ForeColor = Color.FromArgb(30, 30, 46);
            Btn_phimdauxoaall.Margin = new Padding(4);
            Btn_phimdauxoaall.Name = "Btn_phimdauxoaall";
            Btn_phimdauxoaall.TabIndex = 4;
            Btn_phimdauxoaall.Text = "Clear All";
            Btn_phimdauxoaall.UseVisualStyleBackColor = false;
            // 
            // Btn_somot  "1"
            // 
            Btn_somot.BackColor = Color.FromArgb(49, 50, 68);
            Btn_somot.Cursor = Cursors.Hand;
            Btn_somot.Dock = DockStyle.Fill;
            Btn_somot.FlatAppearance.BorderSize = 0;
            Btn_somot.FlatAppearance.MouseDownBackColor = Color.FromArgb(88, 91, 112);
            Btn_somot.FlatAppearance.MouseOverBackColor = Color.FromArgb(69, 71, 90);
            Btn_somot.FlatStyle = FlatStyle.Flat;
            Btn_somot.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            Btn_somot.ForeColor = Color.FromArgb(186, 194, 222);
            Btn_somot.Margin = new Padding(4);
            Btn_somot.Name = "Btn_somot";
            Btn_somot.TabIndex = 5;
            Btn_somot.Text = "1";
            Btn_somot.UseVisualStyleBackColor = false;
            // 
            // Btn_sohai  "2"
            // 
            Btn_sohai.BackColor = Color.FromArgb(49, 50, 68);
            Btn_sohai.Cursor = Cursors.Hand;
            Btn_sohai.Dock = DockStyle.Fill;
            Btn_sohai.FlatAppearance.BorderSize = 0;
            Btn_sohai.FlatAppearance.MouseDownBackColor = Color.FromArgb(88, 91, 112);
            Btn_sohai.FlatAppearance.MouseOverBackColor = Color.FromArgb(69, 71, 90);
            Btn_sohai.FlatStyle = FlatStyle.Flat;
            Btn_sohai.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            Btn_sohai.ForeColor = Color.FromArgb(186, 194, 222);
            Btn_sohai.Margin = new Padding(4);
            Btn_sohai.Name = "Btn_sohai";
            Btn_sohai.TabIndex = 6;
            Btn_sohai.Text = "2";
            Btn_sohai.UseVisualStyleBackColor = false;
            // 
            // Btn_soba  "3"
            // 
            Btn_soba.BackColor = Color.FromArgb(49, 50, 68);
            Btn_soba.Cursor = Cursors.Hand;
            Btn_soba.Dock = DockStyle.Fill;
            Btn_soba.FlatAppearance.BorderSize = 0;
            Btn_soba.FlatAppearance.MouseDownBackColor = Color.FromArgb(88, 91, 112);
            Btn_soba.FlatAppearance.MouseOverBackColor = Color.FromArgb(69, 71, 90);
            Btn_soba.FlatStyle = FlatStyle.Flat;
            Btn_soba.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            Btn_soba.ForeColor = Color.FromArgb(186, 194, 222);
            Btn_soba.Margin = new Padding(4);
            Btn_soba.Name = "Btn_soba";
            Btn_soba.TabIndex = 7;
            Btn_soba.Text = "3";
            Btn_soba.UseVisualStyleBackColor = false;
            // 
            // Btn_phimdaucong  "+"
            // 
            Btn_phimdaucong.BackColor = Color.FromArgb(250, 179, 135);
            Btn_phimdaucong.Cursor = Cursors.Hand;
            Btn_phimdaucong.Dock = DockStyle.Fill;
            Btn_phimdaucong.FlatAppearance.BorderSize = 0;
            Btn_phimdaucong.FlatAppearance.MouseDownBackColor = Color.FromArgb(245, 160, 110);
            Btn_phimdaucong.FlatAppearance.MouseOverBackColor = Color.FromArgb(253, 200, 165);
            Btn_phimdaucong.FlatStyle = FlatStyle.Flat;
            Btn_phimdaucong.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
            Btn_phimdaucong.ForeColor = Color.FromArgb(30, 30, 46);
            Btn_phimdaucong.Margin = new Padding(4);
            Btn_phimdaucong.Name = "Btn_phimdaucong";
            Btn_phimdaucong.TabIndex = 8;
            Btn_phimdaucong.Text = "+";
            Btn_phimdaucong.UseVisualStyleBackColor = false;
            // 
            // Btn_sobon  "4"
            // 
            Btn_sobon.BackColor = Color.FromArgb(49, 50, 68);
            Btn_sobon.Cursor = Cursors.Hand;
            Btn_sobon.Dock = DockStyle.Fill;
            Btn_sobon.FlatAppearance.BorderSize = 0;
            Btn_sobon.FlatAppearance.MouseDownBackColor = Color.FromArgb(88, 91, 112);
            Btn_sobon.FlatAppearance.MouseOverBackColor = Color.FromArgb(69, 71, 90);
            Btn_sobon.FlatStyle = FlatStyle.Flat;
            Btn_sobon.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            Btn_sobon.ForeColor = Color.FromArgb(186, 194, 222);
            Btn_sobon.Margin = new Padding(4);
            Btn_sobon.Name = "Btn_sobon";
            Btn_sobon.TabIndex = 9;
            Btn_sobon.Text = "4";
            Btn_sobon.UseVisualStyleBackColor = false;
            // 
            // Btn_sonam  "5"
            // 
            Btn_sonam.BackColor = Color.FromArgb(49, 50, 68);
            Btn_sonam.Cursor = Cursors.Hand;
            Btn_sonam.Dock = DockStyle.Fill;
            Btn_sonam.FlatAppearance.BorderSize = 0;
            Btn_sonam.FlatAppearance.MouseDownBackColor = Color.FromArgb(88, 91, 112);
            Btn_sonam.FlatAppearance.MouseOverBackColor = Color.FromArgb(69, 71, 90);
            Btn_sonam.FlatStyle = FlatStyle.Flat;
            Btn_sonam.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            Btn_sonam.ForeColor = Color.FromArgb(186, 194, 222);
            Btn_sonam.Margin = new Padding(4);
            Btn_sonam.Name = "Btn_sonam";
            Btn_sonam.TabIndex = 10;
            Btn_sonam.Text = "5";
            Btn_sonam.UseVisualStyleBackColor = false;
            // 
            // Btn_sosau  "6"
            // 
            Btn_sosau.BackColor = Color.FromArgb(49, 50, 68);
            Btn_sosau.Cursor = Cursors.Hand;
            Btn_sosau.Dock = DockStyle.Fill;
            Btn_sosau.FlatAppearance.BorderSize = 0;
            Btn_sosau.FlatAppearance.MouseDownBackColor = Color.FromArgb(88, 91, 112);
            Btn_sosau.FlatAppearance.MouseOverBackColor = Color.FromArgb(69, 71, 90);
            Btn_sosau.FlatStyle = FlatStyle.Flat;
            Btn_sosau.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            Btn_sosau.ForeColor = Color.FromArgb(186, 194, 222);
            Btn_sosau.Margin = new Padding(4);
            Btn_sosau.Name = "Btn_sosau";
            Btn_sosau.TabIndex = 11;
            Btn_sosau.Text = "6";
            Btn_sosau.UseVisualStyleBackColor = false;
            // 
            // Btn_phimdautru  "-"
            // 
            Btn_phimdautru.BackColor = Color.FromArgb(250, 179, 135);
            Btn_phimdautru.Cursor = Cursors.Hand;
            Btn_phimdautru.Dock = DockStyle.Fill;
            Btn_phimdautru.FlatAppearance.BorderSize = 0;
            Btn_phimdautru.FlatAppearance.MouseDownBackColor = Color.FromArgb(245, 160, 110);
            Btn_phimdautru.FlatAppearance.MouseOverBackColor = Color.FromArgb(253, 200, 165);
            Btn_phimdautru.FlatStyle = FlatStyle.Flat;
            Btn_phimdautru.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
            Btn_phimdautru.ForeColor = Color.FromArgb(30, 30, 46);
            Btn_phimdautru.Margin = new Padding(4);
            Btn_phimdautru.Name = "Btn_phimdautru";
            Btn_phimdautru.TabIndex = 12;
            Btn_phimdautru.Text = "-";
            Btn_phimdautru.UseVisualStyleBackColor = false;
            // 
            // Btn_sobay  "7"
            // 
            Btn_sobay.BackColor = Color.FromArgb(49, 50, 68);
            Btn_sobay.Cursor = Cursors.Hand;
            Btn_sobay.Dock = DockStyle.Fill;
            Btn_sobay.FlatAppearance.BorderSize = 0;
            Btn_sobay.FlatAppearance.MouseDownBackColor = Color.FromArgb(88, 91, 112);
            Btn_sobay.FlatAppearance.MouseOverBackColor = Color.FromArgb(69, 71, 90);
            Btn_sobay.FlatStyle = FlatStyle.Flat;
            Btn_sobay.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            Btn_sobay.ForeColor = Color.FromArgb(186, 194, 222);
            Btn_sobay.Margin = new Padding(4);
            Btn_sobay.Name = "Btn_sobay";
            Btn_sobay.TabIndex = 13;
            Btn_sobay.Text = "7";
            Btn_sobay.UseVisualStyleBackColor = false;
            // 
            // Btn_sotam  "8"
            // 
            Btn_sotam.BackColor = Color.FromArgb(49, 50, 68);
            Btn_sotam.Cursor = Cursors.Hand;
            Btn_sotam.Dock = DockStyle.Fill;
            Btn_sotam.FlatAppearance.BorderSize = 0;
            Btn_sotam.FlatAppearance.MouseDownBackColor = Color.FromArgb(88, 91, 112);
            Btn_sotam.FlatAppearance.MouseOverBackColor = Color.FromArgb(69, 71, 90);
            Btn_sotam.FlatStyle = FlatStyle.Flat;
            Btn_sotam.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            Btn_sotam.ForeColor = Color.FromArgb(186, 194, 222);
            Btn_sotam.Margin = new Padding(4);
            Btn_sotam.Name = "Btn_sotam";
            Btn_sotam.TabIndex = 14;
            Btn_sotam.Text = "8";
            Btn_sotam.UseVisualStyleBackColor = false;
            // 
            // Btn_sochin  "9"
            // 
            Btn_sochin.BackColor = Color.FromArgb(49, 50, 68);
            Btn_sochin.Cursor = Cursors.Hand;
            Btn_sochin.Dock = DockStyle.Fill;
            Btn_sochin.FlatAppearance.BorderSize = 0;
            Btn_sochin.FlatAppearance.MouseDownBackColor = Color.FromArgb(88, 91, 112);
            Btn_sochin.FlatAppearance.MouseOverBackColor = Color.FromArgb(69, 71, 90);
            Btn_sochin.FlatStyle = FlatStyle.Flat;
            Btn_sochin.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            Btn_sochin.ForeColor = Color.FromArgb(186, 194, 222);
            Btn_sochin.Margin = new Padding(4);
            Btn_sochin.Name = "Btn_sochin";
            Btn_sochin.TabIndex = 15;
            Btn_sochin.Text = "9";
            Btn_sochin.UseVisualStyleBackColor = false;
            // 
            // Btn_phimdaunhan  "x"
            // 
            Btn_phimdaunhan.BackColor = Color.FromArgb(250, 179, 135);
            Btn_phimdaunhan.Cursor = Cursors.Hand;
            Btn_phimdaunhan.Dock = DockStyle.Fill;
            Btn_phimdaunhan.FlatAppearance.BorderSize = 0;
            Btn_phimdaunhan.FlatAppearance.MouseDownBackColor = Color.FromArgb(245, 160, 110);
            Btn_phimdaunhan.FlatAppearance.MouseOverBackColor = Color.FromArgb(253, 200, 165);
            Btn_phimdaunhan.FlatStyle = FlatStyle.Flat;
            Btn_phimdaunhan.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
            Btn_phimdaunhan.ForeColor = Color.FromArgb(30, 30, 46);
            Btn_phimdaunhan.Margin = new Padding(4);
            Btn_phimdaunhan.Name = "Btn_phimdaunhan";
            Btn_phimdaunhan.TabIndex = 16;
            Btn_phimdaunhan.Text = "x";
            Btn_phimdaunhan.UseVisualStyleBackColor = false;
            // 
            // Btn_sokhong  "0"
            // 
            Btn_sokhong.BackColor = Color.FromArgb(49, 50, 68);
            Btn_sokhong.Cursor = Cursors.Hand;
            Btn_sokhong.Dock = DockStyle.Fill;
            Btn_sokhong.FlatAppearance.BorderSize = 0;
            Btn_sokhong.FlatAppearance.MouseDownBackColor = Color.FromArgb(88, 91, 112);
            Btn_sokhong.FlatAppearance.MouseOverBackColor = Color.FromArgb(69, 71, 90);
            Btn_sokhong.FlatStyle = FlatStyle.Flat;
            Btn_sokhong.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            Btn_sokhong.ForeColor = Color.FromArgb(186, 194, 222);
            Btn_sokhong.Margin = new Padding(4);
            Btn_sokhong.Name = "Btn_sokhong";
            Btn_sokhong.TabIndex = 17;
            Btn_sokhong.Text = "0";
            Btn_sokhong.UseVisualStyleBackColor = false;
            // 
            // Btn_phimdauphay  ","
            // 
            Btn_phimdauphay.BackColor = Color.FromArgb(69, 71, 90);
            Btn_phimdauphay.Cursor = Cursors.Hand;
            Btn_phimdauphay.Dock = DockStyle.Fill;
            Btn_phimdauphay.FlatAppearance.BorderSize = 0;
            Btn_phimdauphay.FlatAppearance.MouseDownBackColor = Color.FromArgb(108, 112, 134);
            Btn_phimdauphay.FlatAppearance.MouseOverBackColor = Color.FromArgb(88, 91, 112);
            Btn_phimdauphay.FlatStyle = FlatStyle.Flat;
            Btn_phimdauphay.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            Btn_phimdauphay.ForeColor = Color.FromArgb(203, 166, 247);
            Btn_phimdauphay.Margin = new Padding(4);
            Btn_phimdauphay.Name = "Btn_phimdauphay";
            Btn_phimdauphay.TabIndex = 18;
            Btn_phimdauphay.Text = ",";
            Btn_phimdauphay.UseVisualStyleBackColor = false;
            // 
            // Btn_phimdauchia  "/"
            // 
            Btn_phimdauchia.BackColor = Color.FromArgb(250, 179, 135);
            Btn_phimdauchia.Cursor = Cursors.Hand;
            Btn_phimdauchia.Dock = DockStyle.Fill;
            Btn_phimdauchia.FlatAppearance.BorderSize = 0;
            Btn_phimdauchia.FlatAppearance.MouseDownBackColor = Color.FromArgb(245, 160, 110);
            Btn_phimdauchia.FlatAppearance.MouseOverBackColor = Color.FromArgb(253, 200, 165);
            Btn_phimdauchia.FlatStyle = FlatStyle.Flat;
            Btn_phimdauchia.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
            Btn_phimdauchia.ForeColor = Color.FromArgb(30, 30, 46);
            Btn_phimdauchia.Margin = new Padding(4);
            Btn_phimdauchia.Name = "Btn_phimdauchia";
            Btn_phimdauchia.TabIndex = 19;
            Btn_phimdauchia.Text = "/";
            Btn_phimdauchia.UseVisualStyleBackColor = false;
            // 
            // Btn_phimdaubang  "="
            // 
            Btn_phimdaubang.BackColor = Color.FromArgb(166, 227, 161);
            Btn_phimdaubang.Cursor = Cursors.Hand;
            Btn_phimdaubang.Dock = DockStyle.Fill;
            Btn_phimdaubang.FlatAppearance.BorderSize = 0;
            Btn_phimdaubang.FlatAppearance.MouseDownBackColor = Color.FromArgb(140, 210, 135);
            Btn_phimdaubang.FlatAppearance.MouseOverBackColor = Color.FromArgb(186, 236, 182);
            Btn_phimdaubang.FlatStyle = FlatStyle.Flat;
            Btn_phimdaubang.Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold);
            Btn_phimdaubang.ForeColor = Color.FromArgb(30, 30, 46);
            Btn_phimdaubang.Margin = new Padding(4);
            Btn_phimdaubang.Name = "Btn_phimdaubang";
            Btn_phimdaubang.TabIndex = 20;
            Btn_phimdaubang.Text = "=";
            Btn_phimdaubang.UseVisualStyleBackColor = false;
            // 
            // Form14
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(24, 24, 37);
            ClientSize = new Size(380, 580);
            Controls.Add(tblMain);
            Controls.Add(pnlStripe);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(2);
            MinimumSize = new Size(340, 520);
            Name = "Form14";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Máy tính cơ bản";
            tblMain.ResumeLayout(false);
            pnlDisplay.ResumeLayout(false);
            tblPhim.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tblMain;
        private TableLayoutPanel tblPhim;
        private Panel pnlStripe;
        private Panel pnlDisplay;
        private Label lblTieuDe;

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