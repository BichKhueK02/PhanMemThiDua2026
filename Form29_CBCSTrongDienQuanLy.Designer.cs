using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using System.Drawing.Drawing2D;
namespace PhanMemThiDua2026
{
    partial class Form29_CBCSTrongDienQuanLy
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
        // Giao diện kiểu "trang web": nền xám nhạt, thẻ trắng bo góc, nút phẳng, lưới gọn.
        // Chỉ dùng control thuần WinForms (không Krypton, không TableLayoutPanel lồng nhau) -> dựng nhanh, vẽ nhẹ.
        // Tên control giữ nguyên để phần code xử lý không đổi.
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form29_CBCSTrongDienQuanLy));
            toolTip1 = new ToolTip(components);
            timer1 = new System.Windows.Forms.Timer(components);
            toolStripStatusLabel4 = new ToolStripStatusLabel();
            pnlHeader = new Panel();
            lblMoTa = new Label();
            lblTieuDe = new Label();
            pnlNutDong = new Panel();
            kryptonButton1_Dong = new NutPhang();
            pnlToolbarHost = new Panel();
            pnlToolbar = new TheBoGoc();
            flowToolbar = new FlowLayoutPanel();
            oTimBoGoc = new OTimBoGoc();
            kryptonButton1_TimKiemThongTinCBCSQuanLy = new NutPhang();
            pnlNganCach = new Panel();
            label4 = new Label();
            comboBox_TimKiemDonVi = new ComboBox();
            label5 = new Label();
            comboBox_XepLoaiThiDua = new ComboBox();
            label1 = new Label();
            comboBox1_GhiChu = new ComboBox();
            kryptonButton_LamMoiCacOTimKiem = new NutPhang();
            pnlLuoiHost = new Panel();
            pnlVienLuoi = new TheBoGoc();
            kryptonDataGridView2 = new LuoiCBCS();
            contextMenuStrip1 = new ContextMenuStrip(components);
            lamMoi_ToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripSeparator();
            xoaTimKiem_ToolStripMenuItem = new ToolStripMenuItem();
            xuatDuLieu_ToolStripMenuItem = new ToolStripMenuItem();
            quayLaiTrangXuLyData_ToolStripMenuItem = new ToolStripMenuItem();
            lblKhongCoDuLieu = new Label();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel_TongSoLoai1 = new ToolStripStatusLabel();
            toolStripStatusLabel_TongSoLoai2 = new ToolStripStatusLabel();
            toolStripStatusLabel_TongSoLoai3 = new ToolStripStatusLabel();
            toolStripStatusLabel_TongSoLoai4 = new ToolStripStatusLabel();
            toolStripStatusLabel_TongSoKhongPhanLoai = new ToolStripStatusLabel();
            pnlHeader.SuspendLayout();
            pnlNutDong.SuspendLayout();
            pnlToolbarHost.SuspendLayout();
            pnlToolbar.SuspendLayout();
            flowToolbar.SuspendLayout();
            pnlLuoiHost.SuspendLayout();
            pnlVienLuoi.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)kryptonDataGridView2).BeginInit();
            kryptonDataGridView2.SuspendLayout();
            contextMenuStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStripStatusLabel4
            // 
            toolStripStatusLabel4.Image = (Image)resources.GetObject("toolStripStatusLabel4.Image");
            toolStripStatusLabel4.Name = "toolStripStatusLabel4";
            toolStripStatusLabel4.Size = new Size(139, 21);
            toolStripStatusLabel4.Text = "toolStripStatusLabel4";
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.FromArgb(243, 244, 246);
            pnlHeader.Controls.Add(lblMoTa);
            pnlHeader.Controls.Add(lblTieuDe);
            pnlHeader.Controls.Add(pnlNutDong);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(1264, 92);
            pnlHeader.TabIndex = 0;
            // 
            // lblMoTa
            // 
            lblMoTa.AutoSize = true;
            lblMoTa.BackColor = Color.FromArgb(243, 244, 246);
            lblMoTa.Font = new Font("Segoe UI", 9F);
            lblMoTa.ForeColor = Color.FromArgb(107, 114, 128);
            lblMoTa.Location = new Point(23, 58);
            lblMoTa.Name = "lblMoTa";
            lblMoTa.Size = new Size(477, 15);
            lblMoTa.TabIndex = 2;
            lblMoTa.Text = "Tìm kiếm, lọc theo đơn vị – xếp loại – ghi chú và xuất danh sách CBCS thuộc diện quản lý";
            // 
            // lblTieuDe
            // 
            lblTieuDe.AutoSize = true;
            lblTieuDe.BackColor = Color.FromArgb(243, 244, 246);
            lblTieuDe.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblTieuDe.ForeColor = Color.FromArgb(124, 58, 237);
            lblTieuDe.Location = new Point(20, 14);
            lblTieuDe.Name = "lblTieuDe";
            lblTieuDe.Size = new Size(292, 32);
            lblTieuDe.TabIndex = 1;
            lblTieuDe.Text = "CBCS trong diện quản lý";
            // 
            // pnlNutDong
            // 
            pnlNutDong.BackColor = Color.FromArgb(243, 244, 246);
            pnlNutDong.Controls.Add(kryptonButton1_Dong);
            pnlNutDong.Dock = DockStyle.Right;
            pnlNutDong.Location = new Point(1134, 0);
            pnlNutDong.Name = "pnlNutDong";
            pnlNutDong.Padding = new Padding(0, 24, 20, 0);
            pnlNutDong.Size = new Size(130, 92);
            pnlNutDong.TabIndex = 0;
            // 
            // kryptonButton1_Dong
            // 
            kryptonButton1_Dong.Dock = DockStyle.Top;
            kryptonButton1_Dong.FlatStyle = FlatStyle.Flat;
            kryptonButton1_Dong.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            kryptonButton1_Dong.Glyph = "";
            kryptonButton1_Dong.Location = new Point(0, 24);
            kryptonButton1_Dong.Name = "kryptonButton1_Dong";
            kryptonButton1_Dong.Primary = false;
            kryptonButton1_Dong.Size = new Size(110, 36);
            kryptonButton1_Dong.TabIndex = 0;
            kryptonButton1_Dong.Text = "Đóng";
            kryptonButton1_Dong.Tim = true;
            kryptonButton1_Dong.UseVisualStyleBackColor = false;
            kryptonButton1_Dong.Click += kryptonButton1_Dong_Click;
            // 
            // pnlToolbarHost
            // 
            pnlToolbarHost.BackColor = Color.FromArgb(243, 244, 246);
            pnlToolbarHost.Controls.Add(pnlToolbar);
            pnlToolbarHost.Dock = DockStyle.Top;
            pnlToolbarHost.Location = new Point(0, 92);
            pnlToolbarHost.Name = "pnlToolbarHost";
            pnlToolbarHost.Padding = new Padding(20, 0, 20, 14);
            pnlToolbarHost.Size = new Size(1264, 80);
            pnlToolbarHost.TabIndex = 1;
            // 
            // pnlToolbar
            // 
            pnlToolbar.BackColor = Color.White;
            pnlToolbar.Controls.Add(flowToolbar);
            pnlToolbar.Dock = DockStyle.Fill;
            pnlToolbar.Location = new Point(20, 0);
            pnlToolbar.Name = "pnlToolbar";
            pnlToolbar.Padding = new Padding(14, 0, 14, 0);
            pnlToolbar.Size = new Size(1224, 66);
            pnlToolbar.TabIndex = 0;
            // 
            // flowToolbar
            // 
            flowToolbar.BackColor = Color.White;
            flowToolbar.Controls.Add(oTimBoGoc);
            flowToolbar.Controls.Add(kryptonButton1_TimKiemThongTinCBCSQuanLy);
            flowToolbar.Controls.Add(pnlNganCach);
            flowToolbar.Controls.Add(label4);
            flowToolbar.Controls.Add(comboBox_TimKiemDonVi);
            flowToolbar.Controls.Add(label5);
            flowToolbar.Controls.Add(comboBox_XepLoaiThiDua);
            flowToolbar.Controls.Add(label1);
            flowToolbar.Controls.Add(comboBox1_GhiChu);
            flowToolbar.Controls.Add(kryptonButton_LamMoiCacOTimKiem);
            flowToolbar.Dock = DockStyle.Fill;
            flowToolbar.Location = new Point(14, 0);
            flowToolbar.Name = "flowToolbar";
            flowToolbar.Size = new Size(1196, 66);
            flowToolbar.TabIndex = 0;
            flowToolbar.WrapContents = false;
            // 
            // oTimBoGoc
            // 
            oTimBoGoc.BackColor = Color.White;
            oTimBoGoc.Location = new Point(0, 12);
            oTimBoGoc.Margin = new Padding(0, 12, 8, 0);
            oTimBoGoc.Name = "oTimBoGoc";
            oTimBoGoc.Size = new Size(260, 36);
            oTimBoGoc.TabIndex = 0;
            // 
            // kryptonButton1_TimKiemThongTinCBCSQuanLy
            // 
            kryptonButton1_TimKiemThongTinCBCSQuanLy.FlatStyle = FlatStyle.Flat;
            kryptonButton1_TimKiemThongTinCBCSQuanLy.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            kryptonButton1_TimKiemThongTinCBCSQuanLy.Glyph = "";
            kryptonButton1_TimKiemThongTinCBCSQuanLy.Location = new Point(268, 12);
            kryptonButton1_TimKiemThongTinCBCSQuanLy.Margin = new Padding(0, 12, 0, 0);
            kryptonButton1_TimKiemThongTinCBCSQuanLy.Name = "kryptonButton1_TimKiemThongTinCBCSQuanLy";
            kryptonButton1_TimKiemThongTinCBCSQuanLy.Primary = true;
            kryptonButton1_TimKiemThongTinCBCSQuanLy.Size = new Size(112, 36);
            kryptonButton1_TimKiemThongTinCBCSQuanLy.TabIndex = 1;
            kryptonButton1_TimKiemThongTinCBCSQuanLy.Text = "Tìm kiếm";
            kryptonButton1_TimKiemThongTinCBCSQuanLy.Tim = false;
            kryptonButton1_TimKiemThongTinCBCSQuanLy.UseVisualStyleBackColor = false;
            kryptonButton1_TimKiemThongTinCBCSQuanLy.Click += kryptonButton1_TimKiemThongTinCBCSQuanLy_Click;
            // 
            // pnlNganCach
            // 
            pnlNganCach.BackColor = Color.FromArgb(229, 231, 235);
            pnlNganCach.Location = new Point(398, 16);
            pnlNganCach.Margin = new Padding(18, 16, 18, 0);
            pnlNganCach.Name = "pnlNganCach";
            pnlNganCach.Size = new Size(1, 32);
            pnlNganCach.TabIndex = 2;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label4.ForeColor = Color.FromArgb(107, 114, 128);
            label4.Location = new Point(417, 23);
            label4.Margin = new Padding(0, 23, 6, 0);
            label4.Name = "label4";
            label4.Size = new Size(42, 15);
            label4.TabIndex = 3;
            label4.Text = "Đơn vị";
            // 
            // comboBox_TimKiemDonVi
            // 
            comboBox_TimKiemDonVi.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_TimKiemDonVi.Font = new Font("Segoe UI", 10F);
            comboBox_TimKiemDonVi.FormattingEnabled = true;
            comboBox_TimKiemDonVi.Location = new Point(465, 17);
            comboBox_TimKiemDonVi.Margin = new Padding(0, 17, 16, 0);
            comboBox_TimKiemDonVi.Name = "comboBox_TimKiemDonVi";
            comboBox_TimKiemDonVi.Size = new Size(120, 25);
            comboBox_TimKiemDonVi.TabIndex = 4;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label5.ForeColor = Color.FromArgb(107, 114, 128);
            label5.Location = new Point(601, 23);
            label5.Margin = new Padding(0, 23, 6, 0);
            label5.Name = "label5";
            label5.Size = new Size(49, 15);
            label5.TabIndex = 5;
            label5.Text = "Xếp loại";
            // 
            // comboBox_XepLoaiThiDua
            // 
            comboBox_XepLoaiThiDua.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_XepLoaiThiDua.Font = new Font("Segoe UI", 10F);
            comboBox_XepLoaiThiDua.FormattingEnabled = true;
            comboBox_XepLoaiThiDua.Location = new Point(656, 17);
            comboBox_XepLoaiThiDua.Margin = new Padding(0, 17, 16, 0);
            comboBox_XepLoaiThiDua.Name = "comboBox_XepLoaiThiDua";
            comboBox_XepLoaiThiDua.Size = new Size(110, 25);
            comboBox_XepLoaiThiDua.TabIndex = 6;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            label1.ForeColor = Color.FromArgb(107, 114, 128);
            label1.Location = new Point(782, 23);
            label1.Margin = new Padding(0, 23, 6, 0);
            label1.Name = "label1";
            label1.Size = new Size(48, 15);
            label1.TabIndex = 7;
            label1.Text = "Ghi chú";
            // 
            // comboBox1_GhiChu
            // 
            comboBox1_GhiChu.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1_GhiChu.Font = new Font("Segoe UI", 10F);
            comboBox1_GhiChu.FormattingEnabled = true;
            comboBox1_GhiChu.Location = new Point(836, 17);
            comboBox1_GhiChu.Margin = new Padding(0, 17, 12, 0);
            comboBox1_GhiChu.Name = "comboBox1_GhiChu";
            comboBox1_GhiChu.Size = new Size(200, 25);
            comboBox1_GhiChu.TabIndex = 8;
            // 
            // kryptonButton_LamMoiCacOTimKiem
            // 
            kryptonButton_LamMoiCacOTimKiem.FlatStyle = FlatStyle.Flat;
            kryptonButton_LamMoiCacOTimKiem.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            kryptonButton_LamMoiCacOTimKiem.Glyph = "";
            kryptonButton_LamMoiCacOTimKiem.Location = new Point(1048, 12);
            kryptonButton_LamMoiCacOTimKiem.Margin = new Padding(0, 12, 0, 0);
            kryptonButton_LamMoiCacOTimKiem.Name = "kryptonButton_LamMoiCacOTimKiem";
            kryptonButton_LamMoiCacOTimKiem.Primary = false;
            kryptonButton_LamMoiCacOTimKiem.Size = new Size(44, 36);
            kryptonButton_LamMoiCacOTimKiem.TabIndex = 9;
            kryptonButton_LamMoiCacOTimKiem.Tim = true;
            kryptonButton_LamMoiCacOTimKiem.UseVisualStyleBackColor = false;
            kryptonButton_LamMoiCacOTimKiem.Click += kryptonButton_LamMoiCacOTimKiem_Click;
            // 
            // pnlLuoiHost
            // 
            pnlLuoiHost.BackColor = Color.FromArgb(243, 244, 246);
            pnlLuoiHost.Controls.Add(pnlVienLuoi);
            pnlLuoiHost.Dock = DockStyle.Fill;
            pnlLuoiHost.Location = new Point(0, 172);
            pnlLuoiHost.Name = "pnlLuoiHost";
            pnlLuoiHost.Padding = new Padding(20, 0, 20, 16);
            pnlLuoiHost.Size = new Size(1264, 483);
            pnlLuoiHost.TabIndex = 2;
            // 
            // pnlVienLuoi
            // 
            pnlVienLuoi.BackColor = Color.White;
            pnlVienLuoi.Controls.Add(kryptonDataGridView2);
            pnlVienLuoi.Dock = DockStyle.Fill;
            pnlVienLuoi.Location = new Point(20, 0);
            pnlVienLuoi.Name = "pnlVienLuoi";
            pnlVienLuoi.Padding = new Padding(6);
            pnlVienLuoi.Size = new Size(1224, 467);
            pnlVienLuoi.TabIndex = 0;
            // 
            // kryptonDataGridView2
            // 
            kryptonDataGridView2.ContextMenuStrip = contextMenuStrip1;
            kryptonDataGridView2.Controls.Add(lblKhongCoDuLieu);
            kryptonDataGridView2.Dock = DockStyle.Fill;
            kryptonDataGridView2.Location = new Point(6, 6);
            kryptonDataGridView2.Name = "kryptonDataGridView2";
            kryptonDataGridView2.Size = new Size(1212, 455);
            kryptonDataGridView2.TabIndex = 0;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Font = new Font("Segoe UI", 9F);
            contextMenuStrip1.ImageScalingSize = new Size(21, 21);
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { lamMoi_ToolStripMenuItem, toolStripMenuItem1, xoaTimKiem_ToolStripMenuItem, xuatDuLieu_ToolStripMenuItem, quayLaiTrangXuLyData_ToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(183, 122);
            // 
            // lamMoi_ToolStripMenuItem
            // 
            lamMoi_ToolStripMenuItem.Image = (Image)resources.GetObject("lamMoi_ToolStripMenuItem.Image");
            lamMoi_ToolStripMenuItem.Name = "lamMoi_ToolStripMenuItem";
            lamMoi_ToolStripMenuItem.ShortcutKeys = Keys.F5;
            lamMoi_ToolStripMenuItem.Size = new Size(182, 28);
            lamMoi_ToolStripMenuItem.Text = "Làm mới trang";
            lamMoi_ToolStripMenuItem.Click += lamMoi_ToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(179, 6);
            // 
            // xoaTimKiem_ToolStripMenuItem
            // 
            xoaTimKiem_ToolStripMenuItem.Image = (Image)resources.GetObject("xoaTimKiem_ToolStripMenuItem.Image");
            xoaTimKiem_ToolStripMenuItem.Name = "xoaTimKiem_ToolStripMenuItem";
            xoaTimKiem_ToolStripMenuItem.ShortcutKeys = Keys.F4;
            xoaTimKiem_ToolStripMenuItem.Size = new Size(182, 28);
            xoaTimKiem_ToolStripMenuItem.Text = "Xóa tìm kiếm";
            xoaTimKiem_ToolStripMenuItem.Click += xoaTimKiem_ToolStripMenuItem_Click;
            // 
            // xuatDuLieu_ToolStripMenuItem
            // 
            xuatDuLieu_ToolStripMenuItem.Image = (Image)resources.GetObject("xuatDuLieu_ToolStripMenuItem.Image");
            xuatDuLieu_ToolStripMenuItem.Name = "xuatDuLieu_ToolStripMenuItem";
            xuatDuLieu_ToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.E;
            xuatDuLieu_ToolStripMenuItem.Size = new Size(182, 28);
            xuatDuLieu_ToolStripMenuItem.Text = "Xuất dữ liệu";
            xuatDuLieu_ToolStripMenuItem.Click += xuatDuLieu_ToolStripMenuItem_Click;
            // 
            // quayLaiTrangXuLyData_ToolStripMenuItem
            // 
            quayLaiTrangXuLyData_ToolStripMenuItem.Image = (Image)resources.GetObject("quayLaiTrangXuLyData_ToolStripMenuItem.Image");
            quayLaiTrangXuLyData_ToolStripMenuItem.Name = "quayLaiTrangXuLyData_ToolStripMenuItem";
            quayLaiTrangXuLyData_ToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Q;
            quayLaiTrangXuLyData_ToolStripMenuItem.Size = new Size(182, 28);
            quayLaiTrangXuLyData_ToolStripMenuItem.Text = "Đóng trang";
            quayLaiTrangXuLyData_ToolStripMenuItem.Click += quayLaiTrangXuLyData_ToolStripMenuItem_Click;
            // 
            // lblKhongCoDuLieu
            // 
            lblKhongCoDuLieu.BackColor = Color.White;
            lblKhongCoDuLieu.Dock = DockStyle.Fill;
            lblKhongCoDuLieu.Font = new Font("Segoe UI", 11F);
            lblKhongCoDuLieu.ForeColor = Color.FromArgb(107, 114, 128);
            lblKhongCoDuLieu.Location = new Point(0, 0);
            lblKhongCoDuLieu.Name = "lblKhongCoDuLieu";
            lblKhongCoDuLieu.Size = new Size(1212, 455);
            lblKhongCoDuLieu.TabIndex = 2;
            lblKhongCoDuLieu.Text = "Không có CBCS nào phù hợp với điều kiện tìm kiếm";
            lblKhongCoDuLieu.TextAlign = ContentAlignment.MiddleCenter;
            lblKhongCoDuLieu.Visible = false;
            // 
            // statusStrip1
            // 
            statusStrip1.BackColor = Color.White;
            statusStrip1.ImageScalingSize = new Size(21, 21);
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel4, toolStripStatusLabel_TongSoLoai1, toolStripStatusLabel_TongSoLoai2, toolStripStatusLabel_TongSoLoai3, toolStripStatusLabel_TongSoLoai4, toolStripStatusLabel_TongSoKhongPhanLoai });
            statusStrip1.Location = new Point(0, 655);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(8, 0, 11, 0);
            statusStrip1.Size = new Size(1264, 26);
            statusStrip1.SizingGrip = false;
            statusStrip1.TabIndex = 3;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel_TongSoLoai1
            // 
            toolStripStatusLabel_TongSoLoai1.Name = "toolStripStatusLabel_TongSoLoai1";
            toolStripStatusLabel_TongSoLoai1.Size = new Size(13, 21);
            toolStripStatusLabel_TongSoLoai1.Text = "1";
            // 
            // toolStripStatusLabel_TongSoLoai2
            // 
            toolStripStatusLabel_TongSoLoai2.Name = "toolStripStatusLabel_TongSoLoai2";
            toolStripStatusLabel_TongSoLoai2.Size = new Size(13, 21);
            toolStripStatusLabel_TongSoLoai2.Text = "2";
            // 
            // toolStripStatusLabel_TongSoLoai3
            // 
            toolStripStatusLabel_TongSoLoai3.Name = "toolStripStatusLabel_TongSoLoai3";
            toolStripStatusLabel_TongSoLoai3.Size = new Size(13, 21);
            toolStripStatusLabel_TongSoLoai3.Text = "3";
            // 
            // toolStripStatusLabel_TongSoLoai4
            // 
            toolStripStatusLabel_TongSoLoai4.Name = "toolStripStatusLabel_TongSoLoai4";
            toolStripStatusLabel_TongSoLoai4.Size = new Size(13, 21);
            toolStripStatusLabel_TongSoLoai4.Text = "4";
            // 
            // toolStripStatusLabel_TongSoKhongPhanLoai
            // 
            toolStripStatusLabel_TongSoKhongPhanLoai.Name = "toolStripStatusLabel_TongSoKhongPhanLoai";
            toolStripStatusLabel_TongSoKhongPhanLoai.Size = new Size(55, 21);
            toolStripStatusLabel_TongSoKhongPhanLoai.Text = "KhongPL";
            // 
            // Form29_CBCSTrongDienQuanLy
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(243, 244, 246);
            ClientSize = new Size(1264, 681);
            ContextMenuStrip = contextMenuStrip1;
            Controls.Add(pnlLuoiHost);
            Controls.Add(pnlToolbarHost);
            Controls.Add(pnlHeader);
            Controls.Add(statusStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(1000, 520);
            Name = "Form29_CBCSTrongDienQuanLy";
            Text = "CBCS trong diện quản lý";
            Load += Form29_CBCSTrongDienQuanLy_Load;
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            pnlNutDong.ResumeLayout(false);
            pnlToolbarHost.ResumeLayout(false);
            pnlToolbar.ResumeLayout(false);
            flowToolbar.ResumeLayout(false);
            flowToolbar.PerformLayout();
            pnlLuoiHost.ResumeLayout(false);
            pnlVienLuoi.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)kryptonDataGridView2).EndInit();
            kryptonDataGridView2.ResumeLayout(false);
            contextMenuStrip1.ResumeLayout(false);
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        private ToolTip toolTip1;
        private System.Windows.Forms.Timer timer1;
        private ToolStripStatusLabel toolStripStatusLabel4;
        private Panel pnlHeader;
        private Panel pnlNutDong;
        private Label lblTieuDe;
        private Label lblMoTa;
        internal NutPhang kryptonButton1_Dong;
        private Panel pnlToolbarHost;
        private TheBoGoc pnlToolbar;
        private FlowLayoutPanel flowToolbar;
        private OTimBoGoc oTimBoGoc;
        internal NutPhang kryptonButton1_TimKiemThongTinCBCSQuanLy;
        private Panel pnlNganCach;
        private Label label4;
        private ComboBox comboBox_TimKiemDonVi;
        private Label label5;
        private ComboBox comboBox_XepLoaiThiDua;
        private Label label1;
        private ComboBox comboBox1_GhiChu;
        private NutPhang kryptonButton_LamMoiCacOTimKiem;
        private Panel pnlLuoiHost;
        private TheBoGoc pnlVienLuoi;
        private LuoiCBCS kryptonDataGridView2;
        private Label lblKhongCoDuLieu;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem lamMoi_ToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem xoaTimKiem_ToolStripMenuItem;
        private ToolStripMenuItem xuatDuLieu_ToolStripMenuItem;
        private ToolStripMenuItem quayLaiTrangXuLyData_ToolStripMenuItem;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel_TongSoLoai1;
        private ToolStripStatusLabel toolStripStatusLabel_TongSoLoai2;
        private ToolStripStatusLabel toolStripStatusLabel_TongSoLoai3;
        private ToolStripStatusLabel toolStripStatusLabel_TongSoLoai4;
        private ToolStripStatusLabel toolStripStatusLabel_TongSoKhongPhanLoai;

        // ====== Bảng màu + control tự vẽ nhẹ (đặt tên riêng để không trùng form khác) ======
        internal static class Mau
        {
            public static readonly Color Nen = Color.FromArgb(243, 244, 246);
            public static readonly Color ChuDam = Color.FromArgb(17, 24, 39);
            public static readonly Color ChuMo = Color.FromArgb(107, 114, 128);
            public static readonly Color VienNhat = Color.FromArgb(229, 231, 235);
            public static readonly Color Xanh = Color.FromArgb(37, 99, 235);
            public static readonly Color XanhHover = Color.FromArgb(29, 78, 216);
            public static readonly Color XanhNhan = Color.FromArgb(30, 64, 175);
            public static readonly Color DongChon = Color.FromArgb(219, 234, 254);
            public static readonly Color DongHover = Color.FromArgb(239, 246, 255);
            public static readonly Color Tim = Color.FromArgb(124, 58, 237);
            public static readonly Color TimChu = Color.FromArgb(91, 33, 182);
            public static readonly Color TimNen = Color.FromArgb(243, 240, 255);
            public static readonly Color TimNenHover = Color.FromArgb(233, 228, 254);
            public static readonly Color TimNenDown = Color.FromArgb(221, 214, 254);
            public static readonly Color TimVien = Color.FromArgb(205, 193, 250);
            public static readonly Color DongLe = Color.FromArgb(249, 250, 251);
        }

        internal static GraphicsPath BoGoc(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>Thẻ trắng bo góc có viền mảnh.</summary>
        internal sealed class TheBoGoc : Panel
        {
            public TheBoGoc()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                BackColor = Color.White;
            }
            protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(Parent?.BackColor ?? Mau.Nen);
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BoGoc(new Rectangle(0, 0, Width - 1, Height - 1), 10);
                using var b = new SolidBrush(Color.White);
                using var p = new Pen(Mau.VienNhat);
                g.FillPath(b, path);
                g.DrawPath(p, path);
            }
        }

        /// <summary>Ô nhập bo tròn: viền xám, khi focus viền chuyển tím. Hop là TextBox bên trong (không viền).</summary>
        internal sealed class OTimBoGoc : Panel
        {
            public readonly TextBox Hop;
            private bool _focus;
            public OTimBoGoc()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                BackColor = Color.White;
                Hop = new TextBox
                {
                    BorderStyle = BorderStyle.None,
                    Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                    BackColor = Color.White,
                    ForeColor = Mau.ChuDam
                };
                Hop.Enter += (s, e) => { _focus = true; Invalidate(); };
                Hop.Leave += (s, e) => { _focus = false; Invalidate(); };
                Controls.Add(Hop);
                Click += (s, e) => Hop.Focus();
            }
            protected override void OnLayout(LayoutEventArgs e)
            {
                base.OnLayout(e);
                if (Hop == null) return;
                int w = Math.Max(10, Width - 28);
                Hop.SetBounds(14, Math.Max(0, (Height - Hop.Height) / 2), w, Hop.Height);
            }
            protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(Parent?.BackColor ?? Color.White);
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BoGoc(new Rectangle(0, 0, Width - 1, Height - 1), 10);
                using var b = new SolidBrush(Color.White);
                using var p = new Pen(_focus ? Mau.Tim : Color.FromArgb(209, 213, 219), _focus ? 1.5F : 1F);
                g.FillPath(b, path);
                g.DrawPath(p, path);
            }
        }

        /// <summary>Nút phẳng bo góc: nút chính (xanh) hoặc nút phụ (trắng viền xám), có icon Segoe MDL2.</summary>
        internal sealed class NutPhang : Button
        {
            private static readonly Font FontIcon = new("Segoe MDL2 Assets", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
            private bool _hover, _down;
            public string Glyph { get; set; } = "";
            public bool Primary { get; set; }
            public bool Tim { get; set; }   // nút nền tím nhạt
            public NutPhang()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;
                UseVisualStyleBackColor = false;
                Cursor = Cursors.Hand;
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point);
            }
            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
            protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Parent?.BackColor ?? Mau.Nen);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color fill, border, fore;
                if (Primary)
                {
                    fill = _down ? Mau.XanhNhan : _hover ? Mau.XanhHover : Mau.Xanh;
                    border = fill; fore = Color.White;
                }
                else if (Tim)
                {
                    fill = _down ? Mau.TimNenDown : _hover ? Mau.TimNenHover : Mau.TimNen;
                    border = Mau.TimVien; fore = Mau.TimChu;
                }
                else
                {
                    fill = _down ? Color.FromArgb(229, 231, 235) : _hover ? Color.FromArgb(243, 244, 246) : Color.White;
                    border = Color.FromArgb(209, 213, 219); fore = Mau.ChuDam;
                }
                using (var path = BoGoc(new Rectangle(0, 0, Width - 1, Height - 1), 8))
                using (var b = new SolidBrush(fill))
                using (var p = new Pen(border))
                {
                    g.FillPath(b, path);
                    g.DrawPath(p, path);
                }
                const TextFormatFlags fl = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left;
                var huge = new Size(int.MaxValue, int.MaxValue);
                Size gs = string.IsNullOrEmpty(Glyph) ? Size.Empty : TextRenderer.MeasureText(g, Glyph, FontIcon, huge, fl);
                Size ts = string.IsNullOrEmpty(Text) ? Size.Empty : TextRenderer.MeasureText(g, Text, Font, huge, fl);
                int gap = gs.Width > 0 && ts.Width > 0 ? 8 : 0;
                int x = (Width - (gs.Width + gap + ts.Width)) / 2;
                if (gs.Width > 0)
                {
                    TextRenderer.DrawText(g, Glyph, FontIcon, new Rectangle(x, 0, gs.Width + 2, Height), fore, fl);
                    x += gs.Width + gap;
                }
                if (ts.Width > 0)
                    TextRenderer.DrawText(g, Text, Font, new Rectangle(x, 0, ts.Width + 2, Height), fore, fl);
            }
        }

        /// <summary>DataGridView thường đã bật double-buffer (nhẹ hơn KryptonDataGridView nhiều khi cuộn/vẽ).</summary>
        internal sealed class LuoiCBCS : DataGridView
        {
            public LuoiCBCS()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            }
        }
    }
}