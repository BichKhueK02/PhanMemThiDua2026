using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Krypton.Toolkit;

namespace PhanMemThiDua2026
{
    partial class Form56_TomTatThanhTichTapTheHangThang
    {
        private IContainer components = null;

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
            components = new Container();
            ComponentResourceManager resources = new ComponentResourceManager(typeof(Form56_TomTatThanhTichTapTheHangThang));
            pnlHeader = new Panel();
            lblMoTa = new Label();
            label3 = new Label();
            pictureBox1 = new PictureBox();
            pnlBody = new Panel();
            pnlCardBorder = new Panel();
            pnlCard = new Panel();
            pnlEditor = new Panel();
            pnlEditBorder = new Panel();
            pnlEditInner = new Panel();
            richTextBox1_TomTatThanhTichTapThe = new RichTextBox();
            pnlFooter = new Panel();
            kryptonButton_LuuDataDeNghi = new KryptonButton();
            lblHint = new Label();
            pnlToolbar = new Panel();
            kryptonButton2_TangCoChuRichText = new KryptonButton();
            kryptonButton2_GiamCoChuRichText = new KryptonButton();
            lblCoChu = new Label();
            lblCardTitle = new Label();
            kryptonStatusStrip1 = new KryptonStatusStrip();
            toolStripStatusLabel1_SoLuongKyTu = new ToolStripStatusLabel();
            toolStripStatusLabel1_ThongBao = new ToolStripStatusLabel();
            toolTip1 = new ToolTip(components);
            pnlHeader.SuspendLayout();
            ((ISupportInitialize)pictureBox1).BeginInit();
            pnlBody.SuspendLayout();
            pnlCardBorder.SuspendLayout();
            pnlCard.SuspendLayout();
            pnlEditor.SuspendLayout();
            pnlEditBorder.SuspendLayout();
            pnlEditInner.SuspendLayout();
            pnlFooter.SuspendLayout();
            pnlToolbar.SuspendLayout();
            kryptonStatusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.FromArgb(49, 46, 129);
            pnlHeader.Controls.Add(lblMoTa);
            pnlHeader.Controls.Add(label3);
            pnlHeader.Controls.Add(pictureBox1);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(1000, 96);
            pnlHeader.TabIndex = 0;
            // 
            // lblMoTa
            // 
            lblMoTa.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblMoTa.BackColor = Color.Transparent;
            lblMoTa.Font = new Font("Segoe UI", 10F);
            lblMoTa.ForeColor = Color.FromArgb(199, 210, 254);
            lblMoTa.Location = new Point(97, 54);
            lblMoTa.Name = "lblMoTa";
            lblMoTa.Size = new Size(871, 22);
            lblMoTa.TabIndex = 2;
            lblMoTa.Text = "Nhập nội dung tóm tắt thành tích hằng tháng của tập thể, sau đó nhấn Lưu để cập nhật hồ sơ đề nghị.";
            lblMoTa.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            label3.BackColor = Color.Transparent;
            label3.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold);
            label3.ForeColor = Color.White;
            label3.Location = new Point(96, 21);
            label3.Name = "label3";
            label3.Size = new Size(872, 32);
            label3.TabIndex = 1;
            label3.Text = "Tóm tắt thành tích tập thể trong phong trào thi đua \"Vì ANTQ\"";
            label3.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pictureBox1
            // 
            pictureBox1.BackColor = Color.Transparent;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(32, 24);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(48, 48);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // pnlBody
            // 
            pnlBody.BackColor = Color.FromArgb(241, 245, 249);
            pnlBody.Controls.Add(pnlCardBorder);
            pnlBody.Dock = DockStyle.Fill;
            pnlBody.Location = new Point(0, 96);
            pnlBody.Name = "pnlBody";
            pnlBody.Padding = new Padding(32, 24, 32, 20);
            pnlBody.Size = new Size(1000, 502);
            pnlBody.TabIndex = 1;
            // 
            // pnlCardBorder
            // 
            pnlCardBorder.BackColor = Color.FromArgb(226, 232, 240);
            pnlCardBorder.Controls.Add(pnlCard);
            pnlCardBorder.Dock = DockStyle.Fill;
            pnlCardBorder.Location = new Point(32, 24);
            pnlCardBorder.Name = "pnlCardBorder";
            pnlCardBorder.Padding = new Padding(1);
            pnlCardBorder.Size = new Size(936, 458);
            pnlCardBorder.TabIndex = 0;
            // 
            // pnlCard
            // 
            pnlCard.BackColor = Color.White;
            pnlCard.Controls.Add(pnlEditor);
            pnlCard.Controls.Add(pnlFooter);
            pnlCard.Controls.Add(pnlToolbar);
            pnlCard.Dock = DockStyle.Fill;
            pnlCard.Location = new Point(1, 1);
            pnlCard.Name = "pnlCard";
            pnlCard.Size = new Size(934, 456);
            pnlCard.TabIndex = 0;
            // 
            // pnlEditor
            // 
            pnlEditor.BackColor = Color.White;
            pnlEditor.Controls.Add(pnlEditBorder);
            pnlEditor.Dock = DockStyle.Fill;
            pnlEditor.Location = new Point(0, 64);
            pnlEditor.Name = "pnlEditor";
            pnlEditor.Padding = new Padding(24, 20, 24, 20);
            pnlEditor.Size = new Size(934, 324);
            pnlEditor.TabIndex = 2;
            // 
            // pnlEditBorder
            // 
            pnlEditBorder.BackColor = Color.FromArgb(203, 213, 225);
            pnlEditBorder.Controls.Add(pnlEditInner);
            pnlEditBorder.Dock = DockStyle.Fill;
            pnlEditBorder.Location = new Point(24, 20);
            pnlEditBorder.Name = "pnlEditBorder";
            pnlEditBorder.Padding = new Padding(1);
            pnlEditBorder.Size = new Size(886, 284);
            pnlEditBorder.TabIndex = 0;
            // 
            // pnlEditInner
            // 
            pnlEditInner.BackColor = Color.White;
            pnlEditInner.Controls.Add(richTextBox1_TomTatThanhTichTapThe);
            pnlEditInner.Dock = DockStyle.Fill;
            pnlEditInner.Location = new Point(1, 1);
            pnlEditInner.Name = "pnlEditInner";
            pnlEditInner.Padding = new Padding(16, 14, 16, 14);
            pnlEditInner.Size = new Size(884, 282);
            pnlEditInner.TabIndex = 0;
            // 
            // richTextBox1_TomTatThanhTichTapThe
            // 
            richTextBox1_TomTatThanhTichTapThe.BackColor = Color.White;
            richTextBox1_TomTatThanhTichTapThe.BorderStyle = BorderStyle.None;
            richTextBox1_TomTatThanhTichTapThe.Dock = DockStyle.Fill;
            richTextBox1_TomTatThanhTichTapThe.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            richTextBox1_TomTatThanhTichTapThe.ForeColor = Color.FromArgb(30, 41, 59);
            richTextBox1_TomTatThanhTichTapThe.Location = new Point(16, 14);
            richTextBox1_TomTatThanhTichTapThe.Name = "richTextBox1_TomTatThanhTichTapThe";
            richTextBox1_TomTatThanhTichTapThe.Size = new Size(852, 254);
            richTextBox1_TomTatThanhTichTapThe.TabIndex = 0;
            richTextBox1_TomTatThanhTichTapThe.Text = "";
            // 
            // pnlFooter
            // 
            pnlFooter.BackColor = Color.FromArgb(248, 250, 252);
            pnlFooter.Controls.Add(kryptonButton_LuuDataDeNghi);
            pnlFooter.Controls.Add(lblHint);
            pnlFooter.Dock = DockStyle.Bottom;
            pnlFooter.Location = new Point(0, 388);
            pnlFooter.Name = "pnlFooter";
            pnlFooter.Size = new Size(934, 68);
            pnlFooter.TabIndex = 1;
            // 
            // kryptonButton_LuuDataDeNghi
            // 
            kryptonButton_LuuDataDeNghi.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            kryptonButton_LuuDataDeNghi.Cursor = Cursors.Hand;
            kryptonButton_LuuDataDeNghi.Location = new Point(764, 12);
            kryptonButton_LuuDataDeNghi.Name = "kryptonButton_LuuDataDeNghi";
            kryptonButton_LuuDataDeNghi.OverrideDefault.Back.Color1 = Color.FromArgb(22, 163, 74);
            kryptonButton_LuuDataDeNghi.OverrideDefault.Back.Color2 = Color.FromArgb(22, 163, 74);
            kryptonButton_LuuDataDeNghi.OverrideDefault.Back.ColorStyle = PaletteColorStyle.Solid;
            kryptonButton_LuuDataDeNghi.OverrideDefault.Border.Color1 = Color.FromArgb(22, 163, 74);
            kryptonButton_LuuDataDeNghi.OverrideDefault.Border.Color2 = Color.FromArgb(22, 163, 74);
            kryptonButton_LuuDataDeNghi.OverrideDefault.Content.ShortText.Color1 = Color.White;
            kryptonButton_LuuDataDeNghi.OverrideDefault.Content.ShortText.Color2 = Color.White;
            kryptonButton_LuuDataDeNghi.Size = new Size(146, 44);
            kryptonButton_LuuDataDeNghi.StateCommon.Back.Color1 = Color.FromArgb(22, 163, 74);
            kryptonButton_LuuDataDeNghi.StateCommon.Back.Color2 = Color.FromArgb(22, 163, 74);
            kryptonButton_LuuDataDeNghi.StateCommon.Back.ColorStyle = PaletteColorStyle.Solid;
            kryptonButton_LuuDataDeNghi.StateCommon.Border.Color1 = Color.FromArgb(22, 163, 74);
            kryptonButton_LuuDataDeNghi.StateCommon.Border.Color2 = Color.FromArgb(22, 163, 74);
            kryptonButton_LuuDataDeNghi.StateCommon.Border.DrawBorders = PaletteDrawBorders.Top | PaletteDrawBorders.Bottom | PaletteDrawBorders.Left | PaletteDrawBorders.Right;
            kryptonButton_LuuDataDeNghi.StateCommon.Border.Rounding = 10F;
            kryptonButton_LuuDataDeNghi.StateCommon.Border.Width = 1;
            kryptonButton_LuuDataDeNghi.StateCommon.Content.ShortText.Color1 = Color.White;
            kryptonButton_LuuDataDeNghi.StateCommon.Content.ShortText.Color2 = Color.White;
            kryptonButton_LuuDataDeNghi.StateCommon.Content.ShortText.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            kryptonButton_LuuDataDeNghi.StateNormal.Back.Color1 = Color.FromArgb(22, 163, 74);
            kryptonButton_LuuDataDeNghi.StateNormal.Back.Color2 = Color.FromArgb(22, 163, 74);
            kryptonButton_LuuDataDeNghi.StateNormal.Content.ShortText.Color1 = Color.White;
            kryptonButton_LuuDataDeNghi.StateNormal.Content.ShortText.Color2 = Color.White;
            kryptonButton_LuuDataDeNghi.StatePressed.Back.Color1 = Color.FromArgb(21, 128, 61);
            kryptonButton_LuuDataDeNghi.StatePressed.Back.Color2 = Color.FromArgb(21, 128, 61);
            kryptonButton_LuuDataDeNghi.StatePressed.Border.Color1 = Color.FromArgb(21, 128, 61);
            kryptonButton_LuuDataDeNghi.StatePressed.Border.Color2 = Color.FromArgb(21, 128, 61);
            kryptonButton_LuuDataDeNghi.StatePressed.Content.ShortText.Color1 = Color.White;
            kryptonButton_LuuDataDeNghi.StatePressed.Content.ShortText.Color2 = Color.White;
            kryptonButton_LuuDataDeNghi.StateTracking.Back.Color1 = Color.FromArgb(34, 197, 94);
            kryptonButton_LuuDataDeNghi.StateTracking.Back.Color2 = Color.FromArgb(34, 197, 94);
            kryptonButton_LuuDataDeNghi.StateTracking.Border.Color1 = Color.FromArgb(34, 197, 94);
            kryptonButton_LuuDataDeNghi.StateTracking.Border.Color2 = Color.FromArgb(34, 197, 94);
            kryptonButton_LuuDataDeNghi.StateTracking.Content.ShortText.Color1 = Color.White;
            kryptonButton_LuuDataDeNghi.StateTracking.Content.ShortText.Color2 = Color.White;
            kryptonButton_LuuDataDeNghi.TabIndex = 2;
            kryptonButton_LuuDataDeNghi.Values.DropDownArrowColor = Color.Empty;
            kryptonButton_LuuDataDeNghi.Values.Image = (Image)resources.GetObject("kryptonButton_LuuDataDeNghi.Values.Image");
            kryptonButton_LuuDataDeNghi.Values.Text = "  Lưu";
            kryptonButton_LuuDataDeNghi.Click += kryptonButton_LuuDataDeNghi_Click;
            // 
            // lblHint
            // 
            lblHint.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblHint.BackColor = Color.Transparent;
            lblHint.Font = new Font("Segoe UI", 9.5F);
            lblHint.ForeColor = Color.FromArgb(100, 116, 139);
            lblHint.Location = new Point(24, 14);
            lblHint.Name = "lblHint";
            lblHint.Size = new Size(720, 40);
            lblHint.TabIndex = 1;
            lblHint.Text = "Gợi ý: trình bày ngắn gọn, nêu bật kết quả nổi bật của tập thể trong tháng.";
            lblHint.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pnlToolbar
            // 
            pnlToolbar.BackColor = Color.FromArgb(248, 250, 252);
            pnlToolbar.Controls.Add(kryptonButton2_TangCoChuRichText);
            pnlToolbar.Controls.Add(kryptonButton2_GiamCoChuRichText);
            pnlToolbar.Controls.Add(lblCoChu);
            pnlToolbar.Controls.Add(lblCardTitle);
            pnlToolbar.Dock = DockStyle.Top;
            pnlToolbar.Location = new Point(0, 0);
            pnlToolbar.Name = "pnlToolbar";
            pnlToolbar.Size = new Size(934, 64);
            pnlToolbar.TabIndex = 0;
            // 
            // kryptonButton2_TangCoChuRichText
            // 
            kryptonButton2_TangCoChuRichText.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            kryptonButton2_TangCoChuRichText.Cursor = Cursors.Hand;
            kryptonButton2_TangCoChuRichText.Location = new Point(868, 15);
            kryptonButton2_TangCoChuRichText.Name = "kryptonButton2_TangCoChuRichText";
            kryptonButton2_TangCoChuRichText.Size = new Size(42, 34);
            kryptonButton2_TangCoChuRichText.StateCommon.Back.Color1 = Color.White;
            kryptonButton2_TangCoChuRichText.StateCommon.Back.Color2 = Color.White;
            kryptonButton2_TangCoChuRichText.StateCommon.Border.Color1 = Color.FromArgb(203, 213, 225);
            kryptonButton2_TangCoChuRichText.StateCommon.Border.Color2 = Color.FromArgb(203, 213, 225);
            kryptonButton2_TangCoChuRichText.StateCommon.Border.DrawBorders = PaletteDrawBorders.Top | PaletteDrawBorders.Bottom | PaletteDrawBorders.Left | PaletteDrawBorders.Right;
            kryptonButton2_TangCoChuRichText.StateCommon.Border.Rounding = 8F;
            kryptonButton2_TangCoChuRichText.StateCommon.Border.Width = 1;
            kryptonButton2_TangCoChuRichText.StateCommon.Content.ShortText.Color1 = Color.FromArgb(51, 65, 85);
            kryptonButton2_TangCoChuRichText.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            kryptonButton2_TangCoChuRichText.StateTracking.Back.Color1 = Color.FromArgb(238, 242, 255);
            kryptonButton2_TangCoChuRichText.StateTracking.Back.Color2 = Color.FromArgb(238, 242, 255);
            kryptonButton2_TangCoChuRichText.StateTracking.Border.Color1 = Color.FromArgb(129, 140, 248);
            kryptonButton2_TangCoChuRichText.StateTracking.Border.Color2 = Color.FromArgb(129, 140, 248);
            kryptonButton2_TangCoChuRichText.TabIndex = 40;
            toolTip1.SetToolTip(kryptonButton2_TangCoChuRichText, "Tăng cỡ chữ");
            kryptonButton2_TangCoChuRichText.Values.DropDownArrowColor = Color.Empty;
            kryptonButton2_TangCoChuRichText.Values.Image = (Image)resources.GetObject("kryptonButton2_TangCoChuRichText.Values.Image");
            kryptonButton2_TangCoChuRichText.Values.Text = "";
            kryptonButton2_TangCoChuRichText.Click += kryptonButton2_TangCoChuRichText_Click;
            // 
            // kryptonButton2_GiamCoChuRichText
            // 
            kryptonButton2_GiamCoChuRichText.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            kryptonButton2_GiamCoChuRichText.Cursor = Cursors.Hand;
            kryptonButton2_GiamCoChuRichText.Location = new Point(820, 15);
            kryptonButton2_GiamCoChuRichText.Name = "kryptonButton2_GiamCoChuRichText";
            kryptonButton2_GiamCoChuRichText.Size = new Size(42, 34);
            kryptonButton2_GiamCoChuRichText.StateCommon.Back.Color1 = Color.White;
            kryptonButton2_GiamCoChuRichText.StateCommon.Back.Color2 = Color.White;
            kryptonButton2_GiamCoChuRichText.StateCommon.Border.Color1 = Color.FromArgb(203, 213, 225);
            kryptonButton2_GiamCoChuRichText.StateCommon.Border.Color2 = Color.FromArgb(203, 213, 225);
            kryptonButton2_GiamCoChuRichText.StateCommon.Border.DrawBorders = PaletteDrawBorders.Top | PaletteDrawBorders.Bottom | PaletteDrawBorders.Left | PaletteDrawBorders.Right;
            kryptonButton2_GiamCoChuRichText.StateCommon.Border.Rounding = 8F;
            kryptonButton2_GiamCoChuRichText.StateCommon.Border.Width = 1;
            kryptonButton2_GiamCoChuRichText.StateCommon.Content.ShortText.Color1 = Color.FromArgb(51, 65, 85);
            kryptonButton2_GiamCoChuRichText.StateCommon.Content.ShortText.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            kryptonButton2_GiamCoChuRichText.StateTracking.Back.Color1 = Color.FromArgb(238, 242, 255);
            kryptonButton2_GiamCoChuRichText.StateTracking.Back.Color2 = Color.FromArgb(238, 242, 255);
            kryptonButton2_GiamCoChuRichText.StateTracking.Border.Color1 = Color.FromArgb(129, 140, 248);
            kryptonButton2_GiamCoChuRichText.StateTracking.Border.Color2 = Color.FromArgb(129, 140, 248);
            kryptonButton2_GiamCoChuRichText.TabIndex = 39;
            toolTip1.SetToolTip(kryptonButton2_GiamCoChuRichText, "Giảm cỡ chữ");
            kryptonButton2_GiamCoChuRichText.Values.DropDownArrowColor = Color.Empty;
            kryptonButton2_GiamCoChuRichText.Values.Image = (Image)resources.GetObject("kryptonButton2_GiamCoChuRichText.Values.Image");
            kryptonButton2_GiamCoChuRichText.Values.Text = "";
            kryptonButton2_GiamCoChuRichText.Click += kryptonButton2_GiamCoChuRichText_Click;
            // 
            // lblCoChu
            // 
            lblCoChu.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblCoChu.BackColor = Color.Transparent;
            lblCoChu.Font = new Font("Segoe UI", 9.5F);
            lblCoChu.ForeColor = Color.FromArgb(100, 116, 139);
            lblCoChu.Location = new Point(740, 14);
            lblCoChu.Name = "lblCoChu";
            lblCoChu.Size = new Size(70, 36);
            lblCoChu.TabIndex = 1;
            lblCoChu.Text = "Cỡ chữ";
            lblCoChu.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblCardTitle
            // 
            lblCardTitle.BackColor = Color.Transparent;
            lblCardTitle.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            lblCardTitle.ForeColor = Color.FromArgb(15, 23, 42);
            lblCardTitle.Location = new Point(24, 14);
            lblCardTitle.Name = "lblCardTitle";
            lblCardTitle.Size = new Size(400, 36);
            lblCardTitle.TabIndex = 0;
            lblCardTitle.Text = "Nội dung tóm tắt";
            lblCardTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // kryptonStatusStrip1
            // 
            kryptonStatusStrip1.BackColor = Color.White;
            kryptonStatusStrip1.Font = new Font("Segoe UI", 9F);
            kryptonStatusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1_SoLuongKyTu, toolStripStatusLabel1_ThongBao });
            kryptonStatusStrip1.Location = new Point(0, 598);
            kryptonStatusStrip1.Name = "kryptonStatusStrip1";
            kryptonStatusStrip1.ProgressBars = null;
            kryptonStatusStrip1.RenderMode = ToolStripRenderMode.ManagerRenderMode;
            kryptonStatusStrip1.Size = new Size(1000, 22);
            kryptonStatusStrip1.TabIndex = 2;
            kryptonStatusStrip1.Text = "kryptonStatusStrip1";
            // 
            // toolStripStatusLabel1_SoLuongKyTu
            // 
            toolStripStatusLabel1_SoLuongKyTu.ForeColor = Color.FromArgb(71, 85, 105);
            toolStripStatusLabel1_SoLuongKyTu.Image = (Image)resources.GetObject("toolStripStatusLabel1_SoLuongKyTu.Image");
            toolStripStatusLabel1_SoLuongKyTu.Name = "toolStripStatusLabel1_SoLuongKyTu";
            toolStripStatusLabel1_SoLuongKyTu.Size = new Size(28, 17);
            toolStripStatusLabel1_SoLuongKyTu.Text = "*";
            // 
            // toolStripStatusLabel1_ThongBao
            // 
            toolStripStatusLabel1_ThongBao.ForeColor = Color.FromArgb(71, 85, 105);
            toolStripStatusLabel1_ThongBao.Name = "toolStripStatusLabel1_ThongBao";
            toolStripStatusLabel1_ThongBao.Size = new Size(62, 17);
            toolStripStatusLabel1_ThongBao.Text = "ThongBao";
            // 
            // Form56_TomTatThanhTichTapTheHangThang
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(241, 245, 249);
            ClientSize = new Size(1000, 620);
            Controls.Add(pnlBody);
            Controls.Add(kryptonStatusStrip1);
            Controls.Add(pnlHeader);
            Font = new Font("Segoe UI", 9.5F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(820, 520);
            Name = "Form56_TomTatThanhTichTapTheHangThang";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Thành tích tập thể";
            Load += Form56_TomTatThanhTichTapTheHangThang_Load;
            pnlHeader.ResumeLayout(false);
            ((ISupportInitialize)pictureBox1).EndInit();
            pnlBody.ResumeLayout(false);
            pnlCardBorder.ResumeLayout(false);
            pnlCard.ResumeLayout(false);
            pnlEditor.ResumeLayout(false);
            pnlEditBorder.ResumeLayout(false);
            pnlEditInner.ResumeLayout(false);
            pnlFooter.ResumeLayout(false);
            pnlToolbar.ResumeLayout(false);
            kryptonStatusStrip1.ResumeLayout(false);
            kryptonStatusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel pnlHeader;
        private Label lblMoTa;
        private Panel pnlBody;
        private Panel pnlCardBorder;
        private Panel pnlCard;
        private Panel pnlToolbar;
        private Label lblCardTitle;
        private Label lblCoChu;
        private Panel pnlFooter;
        private Label lblHint;
        private Panel pnlEditor;
        private Panel pnlEditBorder;
        private Panel pnlEditInner;
        private RichTextBox richTextBox1_TomTatThanhTichTapThe;
        private KryptonButton kryptonButton_LuuDataDeNghi;
        internal KryptonButton kryptonButton2_TangCoChuRichText;
        internal KryptonButton kryptonButton2_GiamCoChuRichText;
        private Label label3;
        private PictureBox pictureBox1;
        private KryptonStatusStrip kryptonStatusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1_SoLuongKyTu;
        private ToolStripStatusLabel toolStripStatusLabel1_ThongBao;
        private ToolTip toolTip1;
    }
}