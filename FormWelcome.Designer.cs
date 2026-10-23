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
            panelCard = new Panel();
            panelNoiDung = new RoundedPanel();
            richTextBox1 = new RichTextBox();
            panelFooter = new Panel();
            btn_BatDau = new Krypton.Toolkit.KryptonButton();
            panelHeader = new Panel();
            pictureBox1 = new PictureBox();
            label1_TenPhanMem = new Label();
            label2_TinCayAnToan = new Label();
            timer1 = new System.Windows.Forms.Timer(components);
            panelCard.SuspendLayout();
            panelNoiDung.SuspendLayout();
            panelFooter.SuspendLayout();
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // panelCard  (phủ kín form, không còn vành ngoài)
            // 
            panelCard.BackColor = Color.FromArgb(30, 41, 59);
            panelCard.Controls.Add(panelNoiDung);
            panelCard.Controls.Add(panelFooter);
            panelCard.Controls.Add(panelHeader);
            panelCard.Dock = DockStyle.Fill;
            panelCard.Location = new Point(0, 0);
            panelCard.Name = "panelCard";
            panelCard.Padding = new Padding(30, 26, 30, 24);
            panelCard.Size = new Size(820, 620);
            panelCard.TabIndex = 0;
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.Transparent;
            panelHeader.Controls.Add(label2_TinCayAnToan);
            panelHeader.Controls.Add(label1_TenPhanMem);
            panelHeader.Controls.Add(pictureBox1);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(30, 26);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(700, 84);
            panelHeader.TabIndex = 1;
            // 
            // pictureBox1
            // 
            pictureBox1.BackColor = Color.Transparent;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(0, 8);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(60, 60);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // label1_TenPhanMem
            // 
            label1_TenPhanMem.AutoSize = true;
            label1_TenPhanMem.BackColor = Color.Transparent;
            label1_TenPhanMem.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1_TenPhanMem.ForeColor = Color.FromArgb(241, 245, 249);
            label1_TenPhanMem.Location = new Point(74, 8);
            label1_TenPhanMem.Name = "label1_TenPhanMem";
            label1_TenPhanMem.Size = new Size(292, 31);
            label1_TenPhanMem.TabIndex = 1;
            label1_TenPhanMem.Text = "PHẦN MỀM THI ĐUA 2026";
            // 
            // label2_TinCayAnToan
            // 
            label2_TinCayAnToan.AutoSize = true;
            label2_TinCayAnToan.BackColor = Color.Transparent;
            label2_TinCayAnToan.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2_TinCayAnToan.ForeColor = Color.FromArgb(148, 163, 184);
            label2_TinCayAnToan.Location = new Point(76, 46);
            label2_TinCayAnToan.Name = "label2_TinCayAnToan";
            label2_TinCayAnToan.Size = new Size(307, 17);
            label2_TinCayAnToan.TabIndex = 2;
            label2_TinCayAnToan.Text = "Tin cậy • An toàn • Bảo mật • Phù hợp mọi máy tính";
            // 
            // panelFooter
            // 
            panelFooter.BackColor = Color.Transparent;
            panelFooter.Controls.Add(btn_BatDau);
            panelFooter.Dock = DockStyle.Bottom;
            panelFooter.Location = new Point(30, 394);
            panelFooter.Name = "panelFooter";
            panelFooter.Padding = new Padding(0, 14, 0, 0);
            panelFooter.Size = new Size(700, 58);
            panelFooter.TabIndex = 3;
            // 
            // btn_BatDau
            // 
            btn_BatDau.Cursor = Cursors.Hand;
            btn_BatDau.Dock = DockStyle.Right;
            btn_BatDau.Location = new Point(530, 14);
            btn_BatDau.Margin = new Padding(3, 2, 3, 2);
            btn_BatDau.Name = "btn_BatDau";
            btn_BatDau.OverrideDefault.Back.Color1 = Color.FromArgb(79, 70, 229);
            btn_BatDau.OverrideDefault.Back.Color2 = Color.FromArgb(79, 70, 229);
            btn_BatDau.OverrideDefault.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn_BatDau.OverrideDefault.Border.Color1 = Color.FromArgb(79, 70, 229);
            btn_BatDau.OverrideDefault.Border.Color2 = Color.FromArgb(79, 70, 229);
            btn_BatDau.OverrideDefault.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            btn_BatDau.OverrideDefault.Border.Rounding = 8F;
            btn_BatDau.OverrideDefault.Border.Width = 1;
            btn_BatDau.OverrideDefault.Content.ShortText.Color1 = Color.White;
            btn_BatDau.OverrideDefault.Content.ShortText.Color2 = Color.White;
            btn_BatDau.OverrideDefault.Content.ShortText.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_BatDau.Size = new Size(170, 44);
            btn_BatDau.StateCommon.Back.Color1 = Color.FromArgb(79, 70, 229);
            btn_BatDau.StateCommon.Back.Color2 = Color.FromArgb(79, 70, 229);
            btn_BatDau.StateCommon.Back.ColorStyle = Krypton.Toolkit.PaletteColorStyle.Solid;
            btn_BatDau.StateCommon.Border.Color1 = Color.FromArgb(79, 70, 229);
            btn_BatDau.StateCommon.Border.Color2 = Color.FromArgb(79, 70, 229);
            btn_BatDau.StateCommon.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            btn_BatDau.StateCommon.Border.Rounding = 8F;
            btn_BatDau.StateCommon.Border.Width = 1;
            btn_BatDau.StateCommon.Content.ShortText.Color1 = Color.White;
            btn_BatDau.StateCommon.Content.ShortText.Color2 = Color.White;
            btn_BatDau.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_BatDau.StatePressed.Back.Color1 = Color.FromArgb(49, 46, 129);
            btn_BatDau.StatePressed.Back.Color2 = Color.FromArgb(49, 46, 129);
            btn_BatDau.StatePressed.Border.Color1 = Color.FromArgb(49, 46, 129);
            btn_BatDau.StatePressed.Border.Color2 = Color.FromArgb(49, 46, 129);
            btn_BatDau.StateTracking.Back.Color1 = Color.FromArgb(67, 56, 202);
            btn_BatDau.StateTracking.Back.Color2 = Color.FromArgb(67, 56, 202);
            btn_BatDau.StateTracking.Border.Color1 = Color.FromArgb(67, 56, 202);
            btn_BatDau.StateTracking.Border.Color2 = Color.FromArgb(67, 56, 202);
            btn_BatDau.TabIndex = 19;
            btn_BatDau.Values.DropDownArrowColor = Color.Empty;
            btn_BatDau.Values.Text = "Bắt đầu";
            // 
            // panelNoiDung
            // 
            panelNoiDung.FillColor = Color.FromArgb(15, 23, 42);
            panelNoiDung.Radius = 14;
            panelNoiDung.Controls.Add(richTextBox1);
            panelNoiDung.Dock = DockStyle.Fill;
            panelNoiDung.Location = new Point(30, 110);
            panelNoiDung.Name = "panelNoiDung";
            panelNoiDung.Padding = new Padding(22, 16, 18, 16);
            panelNoiDung.Size = new Size(700, 284);
            panelNoiDung.TabIndex = 2;
            // 
            // richTextBox1
            // 
            richTextBox1.BackColor = Color.FromArgb(15, 23, 42);
            richTextBox1.BorderStyle = BorderStyle.None;
            richTextBox1.Dock = DockStyle.Fill;
            richTextBox1.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            richTextBox1.ForeColor = Color.FromArgb(203, 213, 225);
            richTextBox1.Location = new Point(22, 16);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.ReadOnly = true;
            richTextBox1.ScrollBars = RichTextBoxScrollBars.Vertical;
            richTextBox1.Size = new Size(660, 252);
            richTextBox1.TabIndex = 20;
            richTextBox1.Text = "";
            // 
            // FormWelcome
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 41, 59);
            ClientSize = new Size(820, 620);
            Controls.Add(panelCard);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormWelcome";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Lời chào đầu tiên";
            Load += FormWelcome_Load;
            panelCard.ResumeLayout(false);
            panelNoiDung.ResumeLayout(false);
            panelFooter.ResumeLayout(false);
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }
        #endregion
        private Panel panelCard;
        private Panel panelHeader;
        private RoundedPanel panelNoiDung;
        private Panel panelFooter;
        private PictureBox pictureBox1;
        private Label label2_TinCayAnToan;
        private Label label1_TenPhanMem;
        internal Krypton.Toolkit.KryptonButton btn_BatDau;
        private RichTextBox richTextBox1;
        private System.Windows.Forms.Timer timer1;
    }
}