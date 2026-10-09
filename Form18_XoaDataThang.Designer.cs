namespace PhanMemThiDua2026
{
    partial class Form18_XoaDataThang
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

        // Bố cục kiểu trang web, toàn bộ dùng Dock nên không bao giờ bị che/cắt:
        //   [ Banner tím  ]  Dock Top     - ảnh + tiêu đề + mô tả
        //   [ Thân xám    ]  Dock Fill    - thẻ trắng bo 12 chứa ô chọn tháng
        //   [ Thanh nút   ]  Dock Bottom  - nút Đóng (trắng) + nút Xóa dữ liệu (đỏ)

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form18_XoaDataThang));

            pnlHeader = new Panel();
            pnlAnh = new RoundedPanel();
            pictureBox1 = new PictureBox();
            lblTieuDe = new Label();
            lblPhuDe = new Label();

            pnlFooter = new Panel();
            pnlFooterLine = new Panel();
            btn_Huy = new Krypton.Toolkit.KryptonButton();
            kryptonButton_XoaDuLieuThangThongKe = new Krypton.Toolkit.KryptonButton();

            pnlBody = new Panel();
            pnlCard = new RoundedPanel();
            label1 = new Label();
            pnlKhungCombo = new Panel();
            comboBox1_ChonThangCanXoaDuLieu = new Krypton.Toolkit.KryptonComboBox();
            lblCanhBao = new Label();

            pnlHeader.SuspendLayout();
            pnlAnh.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            pnlFooter.SuspendLayout();
            pnlBody.SuspendLayout();
            pnlCard.SuspendLayout();
            pnlKhungCombo.SuspendLayout();
            SuspendLayout();

            // ===================== BANNER (tím) =====================
            pnlHeader.BackColor = Color.FromArgb(79, 70, 229);
            pnlHeader.Controls.Add(lblPhuDe);
            pnlHeader.Controls.Add(lblTieuDe);
            pnlHeader.Controls.Add(pnlAnh);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(600, 100);
            pnlHeader.TabIndex = 0;
            //
            // pnlAnh (khung trắng bo tròn chứa ảnh)
            //
            pnlAnh.BackColor = Color.FromArgb(79, 70, 229);
            pnlAnh.BorderColor = Color.White;
            pnlAnh.BorderWidth = 1;
            pnlAnh.FillColor = Color.White;
            pnlAnh.Radius = 14;
            pnlAnh.Controls.Add(pictureBox1);
            pnlAnh.Location = new Point(32, 14);
            pnlAnh.Name = "pnlAnh";
            pnlAnh.Padding = new Padding(8);
            pnlAnh.Size = new Size(72, 72);
            pnlAnh.TabIndex = 0;
            //
            // pictureBox1
            //
            pictureBox1.BackColor = Color.White;
            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Name = "pictureBox1";
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            //
            // lblTieuDe
            //
            lblTieuDe.AutoSize = true;
            lblTieuDe.BackColor = Color.FromArgb(79, 70, 229);
            lblTieuDe.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTieuDe.ForeColor = Color.White;
            lblTieuDe.Location = new Point(120, 20);
            lblTieuDe.Name = "lblTieuDe";
            lblTieuDe.Size = new Size(330, 31);
            lblTieuDe.TabIndex = 1;
            lblTieuDe.Text = "Xóa dữ liệu thi đua tháng";
            //
            // lblPhuDe
            //
            lblPhuDe.AutoSize = true;
            lblPhuDe.BackColor = Color.FromArgb(79, 70, 229);
            lblPhuDe.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblPhuDe.ForeColor = Color.FromArgb(224, 231, 255);
            lblPhuDe.Location = new Point(123, 56);
            lblPhuDe.Name = "lblPhuDe";
            lblPhuDe.Size = new Size(320, 19);
            lblPhuDe.TabIndex = 2;
            lblPhuDe.Text = "Chọn tháng cần xóa khỏi cơ sở dữ liệu thi đua";

            // ===================== THANH NÚT (dưới cùng) =====================
            pnlFooter.BackColor = Color.White;
            pnlFooter.Controls.Add(kryptonButton_XoaDuLieuThangThongKe);
            pnlFooter.Controls.Add(btn_Huy);
            pnlFooter.Controls.Add(pnlFooterLine);
            pnlFooter.Dock = DockStyle.Bottom;
            pnlFooter.Name = "pnlFooter";
            pnlFooter.Size = new Size(600, 76);
            pnlFooter.TabIndex = 2;
            //
            // pnlFooterLine
            //
            pnlFooterLine.BackColor = Color.FromArgb(229, 231, 235);
            pnlFooterLine.Dock = DockStyle.Top;
            pnlFooterLine.Name = "pnlFooterLine";
            pnlFooterLine.Size = new Size(600, 1);
            pnlFooterLine.TabIndex = 2;
            //
            // btn_Huy (nút phụ: trắng, viền xám, đóng form)
            //
            btn_Huy.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btn_Huy.Cursor = Cursors.Hand;
            btn_Huy.DialogResult = DialogResult.Cancel;
            btn_Huy.Location = new Point(298, 17);
            btn_Huy.Margin = new Padding(3, 2, 3, 2);
            btn_Huy.Name = "btn_Huy";
            btn_Huy.Size = new Size(120, 44);
            btn_Huy.TabIndex = 1;
            StyleButton(btn_Huy,
                Color.White, Color.FromArgb(243, 244, 246), Color.FromArgb(229, 231, 235),
                Color.FromArgb(156, 163, 175), Color.FromArgb(55, 65, 81));
            btn_Huy.Values.DropDownArrowColor = Color.Empty;
            btn_Huy.Values.Text = "Đóng";
            //
            // kryptonButton_XoaDuLieuThangThongKe (đỏ, chữ trắng, theo mẫu btn_Luu)
            //
            kryptonButton_XoaDuLieuThangThongKe.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            kryptonButton_XoaDuLieuThangThongKe.Cursor = Cursors.Hand;
            kryptonButton_XoaDuLieuThangThongKe.Location = new Point(430, 17);
            kryptonButton_XoaDuLieuThangThongKe.Margin = new Padding(3, 2, 3, 2);
            kryptonButton_XoaDuLieuThangThongKe.Name = "kryptonButton_XoaDuLieuThangThongKe";
            kryptonButton_XoaDuLieuThangThongKe.Size = new Size(158, 44);
            kryptonButton_XoaDuLieuThangThongKe.TabIndex = 0;
            StyleButton(kryptonButton_XoaDuLieuThangThongKe,
                Color.FromArgb(220, 38, 38), Color.FromArgb(185, 28, 28), Color.FromArgb(153, 27, 27),
                Color.FromArgb(220, 38, 38), Color.White);
            kryptonButton_XoaDuLieuThangThongKe.Values.DropDownArrowColor = Color.Empty;
            kryptonButton_XoaDuLieuThangThongKe.Values.Image = (Image)resources.GetObject("kryptonButton_XoaDuLieuThangThongKe.Values.Image");
            kryptonButton_XoaDuLieuThangThongKe.Values.Text = " Xóa dữ liệu";
            kryptonButton_XoaDuLieuThangThongKe.Click += kryptonButton_XoaDuLieuThangThongKe_Click;

            // ===================== THÂN (xám nhạt) =====================
            pnlBody.BackColor = Color.FromArgb(243, 244, 246);
            pnlBody.Controls.Add(pnlCard);
            pnlBody.Dock = DockStyle.Fill;
            pnlBody.Name = "pnlBody";
            pnlBody.Padding = new Padding(28, 24, 28, 24);
            pnlBody.TabIndex = 1;
            //
            // pnlCard (thẻ trắng bo 12)
            //
            pnlCard.BackColor = Color.White;
            pnlCard.BorderColor = Color.FromArgb(229, 231, 235);
            pnlCard.BorderWidth = 1;
            pnlCard.FillColor = Color.White;
            pnlCard.Radius = 12;
            pnlCard.Controls.Add(lblCanhBao);
            pnlCard.Controls.Add(pnlKhungCombo);
            pnlCard.Controls.Add(label1);
            pnlCard.Dock = DockStyle.Fill;
            pnlCard.Name = "pnlCard";
            pnlCard.Padding = new Padding(24);
            pnlCard.TabIndex = 0;
            //
            // label1 (Dock Top)
            //
            label1.AutoSize = false;
            label1.BackColor = Color.White;
            label1.Dock = DockStyle.Top;
            label1.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.FromArgb(55, 65, 81);
            label1.Name = "label1";
            label1.Size = new Size(100, 28);
            label1.TabIndex = 0;
            label1.Text = "Chọn tháng";
            label1.TextAlign = ContentAlignment.MiddleLeft;
            //
            // pnlKhungCombo (Dock Top, chừa khoảng cách trên/dưới cho ô chọn)
            //
            pnlKhungCombo.BackColor = Color.White;
            pnlKhungCombo.Controls.Add(comboBox1_ChonThangCanXoaDuLieu);
            pnlKhungCombo.Dock = DockStyle.Top;
            pnlKhungCombo.Name = "pnlKhungCombo";
            pnlKhungCombo.Padding = new Padding(0, 8, 0, 8);
            pnlKhungCombo.Size = new Size(100, 66);
            pnlKhungCombo.TabIndex = 1;
            //
            // comboBox1_ChonThangCanXoaDuLieu (KryptonComboBox: tự bo góc, không cần bọc, không tràn)
            //
            comboBox1_ChonThangCanXoaDuLieu.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1_ChonThangCanXoaDuLieu.Dock = DockStyle.Fill;
            comboBox1_ChonThangCanXoaDuLieu.Name = "comboBox1_ChonThangCanXoaDuLieu";
            comboBox1_ChonThangCanXoaDuLieu.Size = new Size(100, 46);
            comboBox1_ChonThangCanXoaDuLieu.StateCommon.ComboBox.Back.Color1 = Color.White;
            comboBox1_ChonThangCanXoaDuLieu.StateCommon.ComboBox.Border.Color1 = Color.FromArgb(209, 213, 219);
            comboBox1_ChonThangCanXoaDuLieu.StateCommon.ComboBox.Border.Color2 = Color.FromArgb(209, 213, 219);
            comboBox1_ChonThangCanXoaDuLieu.StateCommon.ComboBox.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            comboBox1_ChonThangCanXoaDuLieu.StateCommon.ComboBox.Border.Rounding = 8F;
            comboBox1_ChonThangCanXoaDuLieu.StateCommon.ComboBox.Border.Width = 1;
            comboBox1_ChonThangCanXoaDuLieu.StateCommon.ComboBox.Content.Color1 = Color.FromArgb(17, 24, 39);
            comboBox1_ChonThangCanXoaDuLieu.StateCommon.ComboBox.Content.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            // Chỉ chừa lề trái/phải. Padding dọc để -1 (mặc định) vì padding dọc lớn làm khung chữ
            // bên trong bị bóp thấp hơn chiều cao chữ => chữ bị cắt. Chiều cao ô do panel bao quanh quyết định.
            comboBox1_ChonThangCanXoaDuLieu.StateCommon.ComboBox.Content.Padding = new Padding(10, -1, 6, -1);
            // Danh sách thả xuống
            comboBox1_ChonThangCanXoaDuLieu.StateCommon.Item.Content.ShortText.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            comboBox1_ChonThangCanXoaDuLieu.StateCommon.Item.Content.Padding = new Padding(8, 3, 8, 3);
            comboBox1_ChonThangCanXoaDuLieu.DropDownHeight = 320;
            comboBox1_ChonThangCanXoaDuLieu.StateActive.ComboBox.Border.Color1 = Color.FromArgb(79, 70, 229);
            comboBox1_ChonThangCanXoaDuLieu.StateActive.ComboBox.Border.Color2 = Color.FromArgb(79, 70, 229);
            comboBox1_ChonThangCanXoaDuLieu.TabIndex = 0;
            //
            // lblCanhBao (Dock Top)
            //
            lblCanhBao.AutoSize = false;
            lblCanhBao.BackColor = Color.White;
            lblCanhBao.Dock = DockStyle.Top;
            lblCanhBao.Font = new Font("Segoe UI", 9.5F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblCanhBao.ForeColor = Color.FromArgb(185, 28, 28);
            lblCanhBao.Name = "lblCanhBao";
            lblCanhBao.Size = new Size(100, 30);
            lblCanhBao.TabIndex = 2;
            lblCanhBao.Text = "Lưu ý: dữ liệu đã xóa không thể khôi phục.";
            lblCanhBao.TextAlign = ContentAlignment.MiddleLeft;

            // ===================== FORM =====================
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            CancelButton = btn_Huy;
            ClientSize = new Size(600, 400);
            Controls.Add(pnlBody);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form18_XoaDataThang";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Xóa dữ liệu thi đua tháng";
            Load += Form18_XoaDataThang_Load;

            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            pnlAnh.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            pnlFooter.ResumeLayout(false);
            pnlBody.ResumeLayout(false);
            pnlCard.ResumeLayout(false);
            pnlCard.PerformLayout();
            pnlKhungCombo.ResumeLayout(false);
            ResumeLayout(false);
        }

        // Nút bo 8, cấu trúc giống btn_Luu mẫu (OverrideDefault + StateCommon/Tracking/Pressed)
        private void StyleButton(Krypton.Toolkit.KryptonButton b, Color back, Color hover, Color press,
                                 Color border, Color text)
        {
            var solid = Krypton.Toolkit.PaletteColorStyle.Solid;
            var all = Krypton.Toolkit.PaletteDrawBorders.Top | Krypton.Toolkit.PaletteDrawBorders.Bottom |
                      Krypton.Toolkit.PaletteDrawBorders.Left | Krypton.Toolkit.PaletteDrawBorders.Right;
            var font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 0);

            b.OverrideDefault.Back.Color1 = back;
            b.OverrideDefault.Back.Color2 = back;
            b.OverrideDefault.Back.ColorStyle = solid;
            b.OverrideDefault.Border.Color1 = border;
            b.OverrideDefault.Border.Color2 = border;
            b.OverrideDefault.Border.DrawBorders = all;
            b.OverrideDefault.Border.Rounding = 8F;
            b.OverrideDefault.Border.Width = 1;
            b.OverrideDefault.Content.ShortText.Color1 = text;
            b.OverrideDefault.Content.ShortText.Color2 = text;
            b.OverrideDefault.Content.ShortText.Font = font;

            b.StateCommon.Back.Color1 = back;
            b.StateCommon.Back.Color2 = back;
            b.StateCommon.Back.ColorStyle = solid;
            b.StateCommon.Border.Color1 = border;
            b.StateCommon.Border.Color2 = border;
            b.StateCommon.Border.DrawBorders = all;
            b.StateCommon.Border.Rounding = 8F;
            b.StateCommon.Border.Width = 1;
            b.StateCommon.Content.ShortText.Color1 = text;
            b.StateCommon.Content.ShortText.Color2 = text;
            b.StateCommon.Content.ShortText.Font = font;

            b.StateTracking.Back.Color1 = hover;
            b.StateTracking.Back.Color2 = hover;
            b.StateTracking.Border.Color1 = border;
            b.StateTracking.Border.Color2 = border;

            b.StatePressed.Back.Color1 = press;
            b.StatePressed.Back.Color2 = press;
            b.StatePressed.Border.Color1 = border;
            b.StatePressed.Border.Color2 = border;
        }

        #endregion

        private Panel pnlHeader;
        private RoundedPanel pnlAnh;
        private PictureBox pictureBox1;
        private Label lblTieuDe;
        private Label lblPhuDe;

        private Panel pnlFooter;
        private Panel pnlFooterLine;
        private Krypton.Toolkit.KryptonButton btn_Huy;
        internal Krypton.Toolkit.KryptonButton kryptonButton_XoaDuLieuThangThongKe;

        private Panel pnlBody;
        private RoundedPanel pnlCard;
        private Label label1;
        private Panel pnlKhungCombo;
        private Krypton.Toolkit.KryptonComboBox comboBox1_ChonThangCanXoaDuLieu;
        private Label lblCanhBao;
    }
}