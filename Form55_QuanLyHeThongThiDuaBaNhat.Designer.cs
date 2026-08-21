namespace PhanMemThiDua2026
{
    partial class Form55_QuanLyHeThongThiDuaBaNhat
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form55_QuanLyHeThongThiDuaBaNhat));
            panelContent = new Panel();
            menuStrip1 = new MenuStrip();
            quanLyThiDuaBaNhat_ToolStripMenuItem = new ToolStripMenuItem();
            quanLySoVangThiDuaBaNhat_ToolStripMenuItem = new ToolStripMenuItem();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // panelContent
            // 
            panelContent.Dock = DockStyle.Fill;
            panelContent.Location = new Point(0, 29);
            panelContent.Name = "panelContent";
            panelContent.Size = new Size(1264, 652);
            panelContent.TabIndex = 5;
            // 
            // menuStrip1
            // 
            menuStrip1.BackColor = Color.FromArgb(192, 192, 255);
            menuStrip1.Font = new Font("Segoe UI", 9F);
            menuStrip1.Items.AddRange(new ToolStripItem[] { quanLyThiDuaBaNhat_ToolStripMenuItem, quanLySoVangThiDuaBaNhat_ToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1264, 29);
            menuStrip1.TabIndex = 4;
            menuStrip1.Text = "menuStrip1";
            // 
            // quanLyThiDuaBaNhat_ToolStripMenuItem
            // 
            quanLyThiDuaBaNhat_ToolStripMenuItem.Font = new Font("Segoe UI", 12F);
            quanLyThiDuaBaNhat_ToolStripMenuItem.Image = (Image)resources.GetObject("quanLyThiDuaBaNhat_ToolStripMenuItem.Image");
            quanLyThiDuaBaNhat_ToolStripMenuItem.Name = "quanLyThiDuaBaNhat_ToolStripMenuItem";
            quanLyThiDuaBaNhat_ToolStripMenuItem.Size = new Size(200, 25);
            quanLyThiDuaBaNhat_ToolStripMenuItem.Text = "Quản lý thi đua Ba nhất";
            quanLyThiDuaBaNhat_ToolStripMenuItem.Click += quanLyThiDuaBaNhat_ToolStripMenuItem_Click;
            // 
            // quanLySoVangThiDuaBaNhat_ToolStripMenuItem
            // 
            quanLySoVangThiDuaBaNhat_ToolStripMenuItem.Font = new Font("Segoe UI", 12F);
            quanLySoVangThiDuaBaNhat_ToolStripMenuItem.Image = (Image)resources.GetObject("quanLySoVangThiDuaBaNhat_ToolStripMenuItem.Image");
            quanLySoVangThiDuaBaNhat_ToolStripMenuItem.Name = "quanLySoVangThiDuaBaNhat_ToolStripMenuItem";
            quanLySoVangThiDuaBaNhat_ToolStripMenuItem.Size = new Size(202, 25);
            quanLySoVangThiDuaBaNhat_ToolStripMenuItem.Text = "Sổ vàng thi đua Ba nhất";
            quanLySoVangThiDuaBaNhat_ToolStripMenuItem.Click += quanLySoVangThiDuaBaNhat_ToolStripMenuItem_Click;
            // 
            // Form55_QuanLyHeThongThiDuaBaNhat
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1264, 681);
            Controls.Add(panelContent);
            Controls.Add(menuStrip1);
            Name = "Form55_QuanLyHeThongThiDuaBaNhat";
            Text = "Form55_QuanLyHeThongThiDuaBaNhat";
            Load += Form55_QuanLyHeThongThiDuaBaNhat_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panelContent;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem quanLyThiDuaBaNhat_ToolStripMenuItem;
        private ToolStripMenuItem quanLySoVangThiDuaBaNhat_ToolStripMenuItem;
    }
}