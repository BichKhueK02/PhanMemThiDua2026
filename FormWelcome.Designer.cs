namespace PhanMemThiDua2026
{
    partial class FormWelcome
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormWelcome));

            panelNen = new Panel();
            panelCard = new Panel();
            panelNoiDung = new Panel();
            richTextBox1 = new RichTextBox();
            panelFooter = new Panel();
            btn_BatDau = new Krypton.Toolkit.KryptonButton();
            panelHeader = new Panel();
            tableLayoutPanel2 = new TableLayoutPanel();
            pictureBox1 = new PictureBox();
            tableLayoutPanel3 = new TableLayoutPanel();
            label1_TenPhanMem = new Label();
            label2_TinCayAnToan = new Label();
            panelThanhMau = new Panel();
            timer1 = new System.Windows.Forms.Timer(components);

            panelNen.SuspendLayout();
            panelCard.SuspendLayout();
            panelNoiDung.SuspendLayout();
            panelFooter.SuspendLayout();
            panelHeader.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            tableLayoutPanel3.SuspendLayout();
            SuspendLayout();

            // 
            // panelNen
            // 
            panelNen.BackColor = Color.FromArgb(246, 248, 251);
            panelNen.Controls.Add(panelCard);
            panelNen.Dock = DockStyle.Fill;
            panelNen.Location = new Point(0, 0);
            panelNen.Name = "panelNen";
            panelNen.Padding = new Padding(30);
            panelNen.Size = new Size(850, 520);
            panelNen.TabIndex = 0;

            // 
            // panelCard
            // 
            panelCard.BackColor = Color.White;
            panelCard.Controls.Add(panelNoiDung);
            panelCard.Controls.Add(panelFooter);
            panelCard.Controls.Add(panelHeader);
            panelCard.Controls.Add(panelThanhMau);
            panelCard.Dock = DockStyle.Fill;
            panelCard.Location = new Point(30, 30);
            panelCard.Name = "panelCard";
            panelCard.Size = new Size(790, 460);
            panelCard.TabIndex = 0;

            // 
            // panelThanhMau
            // 
            panelThanhMau.BackColor = Color.FromArgb(0, 120, 215);
            panelThanhMau.Dock = DockStyle.Top;
            panelThanhMau.Location = new Point(0, 0);
            panelThanhMau.Name = "panelThanhMau";
            panelThanhMau.Size = new Size(790, 4);
            panelThanhMau.TabIndex = 0;

            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.White;
            panelHeader.Controls.Add(tableLayoutPanel2);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 4);
            panelHeader.Name = "panelHeader";
            panelHeader.Padding = new Padding(34, 22, 34, 18);
            panelHeader.Size = new Size(790, 112);
            panelHeader.TabIndex = 1;

            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 2;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Controls.Add(pictureBox1, 0, 0);
            tableLayoutPanel2.Controls.Add(tableLayoutPanel3, 1, 0);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(34, 22);
            tableLayoutPanel2.Margin = new Padding(0);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Size = new Size(722, 72);
            tableLayoutPanel2.TabIndex = 0;

            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.None;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(6, 6);
            pictureBox1.Margin = new Padding(0);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(60, 60);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;

            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.ColumnCount = 1;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.Controls.Add(label1_TenPhanMem, 0, 0);
            tableLayoutPanel3.Controls.Add(label2_TinCayAnToan, 0, 1);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new Point(72, 0);
            tableLayoutPanel3.Margin = new Padding(0);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 2;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            tableLayoutPanel3.Size = new Size(650, 72);
            tableLayoutPanel3.TabIndex = 1;

            // 
            // label1_TenPhanMem
            // 
            label1_TenPhanMem.Anchor = AnchorStyles.Left;
            label1_TenPhanMem.AutoSize = true;
            label1_TenPhanMem.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1_TenPhanMem.ForeColor = Color.FromArgb(15, 23, 42);
            label1_TenPhanMem.Location = new Point(0, 3);
            label1_TenPhanMem.Margin = new Padding(0);
            label1_TenPhanMem.Name = "label1_TenPhanMem";
            label1_TenPhanMem.Size = new Size(295, 31);
            label1_TenPhanMem.TabIndex = 1;
            label1_TenPhanMem.Text = "PHẦN MỀM THI ĐUA 2026";
            label1_TenPhanMem.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // label2_TinCayAnToan
            // 
            label2_TinCayAnToan.Anchor = AnchorStyles.Left;
            label2_TinCayAnToan.AutoSize = true;
            label2_TinCayAnToan.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2_TinCayAnToan.ForeColor = Color.FromArgb(100, 116, 139);
            label2_TinCayAnToan.Location = new Point(0, 49);
            label2_TinCayAnToan.Margin = new Padding(0);
            label2_TinCayAnToan.Name = "label2_TinCayAnToan";
            label2_TinCayAnToan.Size = new Size(307, 17);
            label2_TinCayAnToan.TabIndex = 2;
            label2_TinCayAnToan.Text = "Tin cậy • An toàn • Bảo mật • Phù hợp mọi máy tính";
            label2_TinCayAnToan.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // panelNoiDung
            // 
            panelNoiDung.BackColor = Color.FromArgb(248, 250, 252);
            panelNoiDung.Controls.Add(richTextBox1);
            panelNoiDung.Dock = DockStyle.Fill;
            panelNoiDung.Location = new Point(0, 116);
            panelNoiDung.Name = "panelNoiDung";
            panelNoiDung.Padding = new Padding(34, 18, 34, 14);
            panelNoiDung.Size = new Size(790, 270);
            panelNoiDung.TabIndex = 2;

            // 
            // richTextBox1
            // 
            richTextBox1.BackColor = Color.White;
            richTextBox1.BorderStyle = BorderStyle.None;
            richTextBox1.Dock = DockStyle.Fill;
            richTextBox1.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            richTextBox1.ForeColor = Color.FromArgb(51, 65, 85);
            richTextBox1.Location = new Point(34, 18);
            richTextBox1.Margin = new Padding(0);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.ReadOnly = true;
            richTextBox1.ScrollBars = RichTextBoxScrollBars.Vertical;
            richTextBox1.Size = new Size(722, 238);
            richTextBox1.TabIndex = 20;
            richTextBox1.Text = "";

            // 
            // panelFooter
            // 
            panelFooter.BackColor = Color.White;
            panelFooter.Controls.Add(btn_BatDau);
            panelFooter.Dock = DockStyle.Bottom;
            panelFooter.Location = new Point(0, 386);
            panelFooter.Name = "panelFooter";
            panelFooter.Padding = new Padding(34, 12, 34, 20);
            panelFooter.Size = new Size(790, 74);
            panelFooter.TabIndex = 3;

            // 
            // btn_BatDau
            // 
            btn_BatDau.Cursor = Cursors.Hand;
            btn_BatDau.Dock = DockStyle.Right;
            btn_BatDau.Location = new Point(621, 12);
            btn_BatDau.Margin = new Padding(0);
            btn_BatDau.Name = "btn_BatDau";
            btn_BatDau.OverrideDefault.Back.Color1 = Color.FromArgb(0, 120, 215);
            btn_BatDau.OverrideDefault.Back.Color2 = Color.FromArgb(0, 120, 215);
            btn_BatDau.OverrideDefault.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn_BatDau.OverrideDefault.Border.Color1 = Color.FromArgb(0, 120, 215);
            btn_BatDau.OverrideDefault.Border.Color2 = Color.FromArgb(0, 120, 215);
            btn_BatDau.OverrideDefault.Border.DrawBorders =
                Krypton.Toolkit.PaletteDrawBorders.Top |
                Krypton.Toolkit.PaletteDrawBorders.Bottom |
                Krypton.Toolkit.PaletteDrawBorders.Left |
                Krypton.Toolkit.PaletteDrawBorders.Right;
            btn_BatDau.OverrideDefault.Border.Rounding = 7F;
            btn_BatDau.OverrideDefault.Border.Width = 1;
            btn_BatDau.OverrideDefault.Content.ShortText.Color1 = Color.White;
            btn_BatDau.OverrideDefault.Content.ShortText.Color2 = Color.White;
            btn_BatDau.OverrideDefault.Content.ShortText.Font =
                new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);

            btn_BatDau.Size = new Size(135, 42);

            btn_BatDau.StateCommon.Back.Color1 = Color.FromArgb(0, 120, 215);
            btn_BatDau.StateCommon.Back.Color2 = Color.FromArgb(0, 120, 215);
            btn_BatDau.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn_BatDau.StateCommon.Border.Color1 = Color.FromArgb(0, 120, 215);
            btn_BatDau.StateCommon.Border.Color2 = Color.FromArgb(0, 120, 215);
            btn_BatDau.StateCommon.Border.DrawBorders =
                Krypton.Toolkit.PaletteDrawBorders.Top |
                Krypton.Toolkit.PaletteDrawBorders.Bottom |
                Krypton.Toolkit.PaletteDrawBorders.Left |
                Krypton.Toolkit.PaletteDrawBorders.Right;
            btn_BatDau.StateCommon.Border.Rounding = 7F;
            btn_BatDau.StateCommon.Border.Width = 1;
            btn_BatDau.StateCommon.Content.ShortText.Color1 = Color.White;
            btn_BatDau.StateCommon.Content.ShortText.Color2 = Color.White;
            btn_BatDau.StateCommon.Content.ShortText.Font =
                new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);

            btn_BatDau.StateTracking.Back.Color1 = Color.FromArgb(0, 102, 204);
            btn_BatDau.StateTracking.Back.Color2 = Color.FromArgb(0, 102, 204);
            btn_BatDau.StateTracking.Border.Color1 = Color.FromArgb(0, 102, 204);
            btn_BatDau.StateTracking.Border.Color2 = Color.FromArgb(0, 102, 204);

            btn_BatDau.StatePressed.Back.Color1 = Color.FromArgb(0, 84, 166);
            btn_BatDau.StatePressed.Back.Color2 = Color.FromArgb(0, 84, 166);
            btn_BatDau.StatePressed.Border.Color1 = Color.FromArgb(0, 84, 166);
            btn_BatDau.StatePressed.Border.Color2 = Color.FromArgb(0, 84, 166);

            btn_BatDau.TabIndex = 19;
            btn_BatDau.Values.DropDownArrowColor = Color.Empty;
            btn_BatDau.Values.Image = (Image)resources.GetObject("btn_BatDau.Values.Image");
            btn_BatDau.Values.Text = "Bắt đầu";

            // 
            // FormWelcome
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(246, 248, 251);
            ClientSize = new Size(850, 520);
            Controls.Add(panelNen);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormWelcome";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Lời chào đầu tiên";
            Load += FormWelcome_Load;

            panelNen.ResumeLayout(false);
            panelCard.ResumeLayout(false);
            panelNoiDung.ResumeLayout(false);
            panelFooter.ResumeLayout(false);
            panelHeader.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            tableLayoutPanel3.ResumeLayout(false);
            tableLayoutPanel3.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panelNen;
        private Panel panelCard;
        private Panel panelThanhMau;
        private Panel panelHeader;
        private Panel panelNoiDung;
        private Panel panelFooter;
        private TableLayoutPanel tableLayoutPanel2;
        private PictureBox pictureBox1;
        private TableLayoutPanel tableLayoutPanel3;
        private Label label2_TinCayAnToan;
        private Label label1_TenPhanMem;
        internal Krypton.Toolkit.KryptonButton btn_BatDau;
        private RichTextBox richTextBox1;
        private System.Windows.Forms.Timer timer1;
    }
}