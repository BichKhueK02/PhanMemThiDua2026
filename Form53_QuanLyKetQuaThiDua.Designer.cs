namespace PhanMemThiDua2026
{
    partial class Form53_QuanLyKetQuaThiDua
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form53_QuanLyKetQuaThiDua));
            panelContent = new Panel();
            menuStrip1 = new MenuStrip();
            quanLyThiDuaNamHienTai_ToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripMenuItem();
            quanLyThiDuaNamCu_ToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem2 = new ToolStripMenuItem();
            toolStripMenuItem3_QuanLyThiDuaTapTheNamCu = new ToolStripMenuItem();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // panelContent
            // 
            panelContent.Dock = DockStyle.Fill;
            panelContent.Location = new Point(0, 29);
            panelContent.Name = "panelContent";
            panelContent.Size = new Size(1264, 652);
            panelContent.TabIndex = 3;
            // 
            // menuStrip1
            // 
            menuStrip1.BackColor = Color.FromArgb(192, 192, 255);
            menuStrip1.Font = new Font("Segoe UI", 9F);
            menuStrip1.Items.AddRange(new ToolStripItem[] { quanLyThiDuaNamHienTai_ToolStripMenuItem, toolStripMenuItem1, quanLyThiDuaNamCu_ToolStripMenuItem, toolStripMenuItem2, toolStripMenuItem3_QuanLyThiDuaTapTheNamCu });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1264, 29);
            menuStrip1.TabIndex = 2;
            menuStrip1.Text = "menuStrip1";
            // 
            // quanLyThiDuaNamHienTai_ToolStripMenuItem
            // 
            quanLyThiDuaNamHienTai_ToolStripMenuItem.Font = new Font("Segoe UI", 12F);
            quanLyThiDuaNamHienTai_ToolStripMenuItem.Image = (Image)resources.GetObject("quanLyThiDuaNamHienTai_ToolStripMenuItem.Image");
            quanLyThiDuaNamHienTai_ToolStripMenuItem.Name = "quanLyThiDuaNamHienTai_ToolStripMenuItem";
            quanLyThiDuaNamHienTai_ToolStripMenuItem.Size = new Size(234, 25);
            quanLyThiDuaNamHienTai_ToolStripMenuItem.Text = "Quản lý thi đua năm hiện tại";
            quanLyThiDuaNamHienTai_ToolStripMenuItem.Click += quanLyThiDuaNamHienTai_ToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(22, 25);
            toolStripMenuItem1.Text = "|";
            // 
            // quanLyThiDuaNamCu_ToolStripMenuItem
            // 
            quanLyThiDuaNamCu_ToolStripMenuItem.Font = new Font("Segoe UI", 12F);
            quanLyThiDuaNamCu_ToolStripMenuItem.Image = (Image)resources.GetObject("quanLyThiDuaNamCu_ToolStripMenuItem.Image");
            quanLyThiDuaNamCu_ToolStripMenuItem.Name = "quanLyThiDuaNamCu_ToolStripMenuItem";
            quanLyThiDuaNamCu_ToolStripMenuItem.Size = new Size(241, 25);
            quanLyThiDuaNamCu_ToolStripMenuItem.Text = "Quản lý thi đua CBCS năm cũ";
            quanLyThiDuaNamCu_ToolStripMenuItem.Click += quanLyThiDuaNamCu_ToolStripMenuItem_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new Size(22, 25);
            toolStripMenuItem2.Text = "|";
            // 
            // toolStripMenuItem3_QuanLyThiDuaTapTheNamCu
            // 
            toolStripMenuItem3_QuanLyThiDuaTapTheNamCu.Font = new Font("Segoe UI", 12F);
            toolStripMenuItem3_QuanLyThiDuaTapTheNamCu.Image = (Image)resources.GetObject("toolStripMenuItem3_QuanLyThiDuaTapTheNamCu.Image");
            toolStripMenuItem3_QuanLyThiDuaTapTheNamCu.Name = "toolStripMenuItem3_QuanLyThiDuaTapTheNamCu";
            toolStripMenuItem3_QuanLyThiDuaTapTheNamCu.Size = new Size(251, 25);
            toolStripMenuItem3_QuanLyThiDuaTapTheNamCu.Text = "Quản lý thi đua tập thể năm cũ";
            toolStripMenuItem3_QuanLyThiDuaTapTheNamCu.Click += toolStripMenuItem3_QuanLyThiDuaTapTheNamCu_Click;
            // 
            // Form53_QuanLyKetQuaThiDua
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1264, 681);
            Controls.Add(panelContent);
            Controls.Add(menuStrip1);
            Name = "Form53_QuanLyKetQuaThiDua";
            Text = "Form53_QuanLyKetQuaThiDua";
            Load += Form53_QuanLyKetQuaThiDua_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panelContent;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem quanLyThiDuaNamHienTai_ToolStripMenuItem;
        private ToolStripMenuItem quanLyThiDuaNamCu_ToolStripMenuItem;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem toolStripMenuItem2;
        private ToolStripMenuItem toolStripMenuItem3_QuanLyThiDuaTapTheNamCu;
    }
}