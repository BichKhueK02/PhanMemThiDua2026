using Krypton.Toolkit;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PhanMemThiDua2026
{
    /// <summary>
    /// Giao diện kiểu web cho Form22_ChinhSuaDataCBCS (bản ngang).
    /// - Chỉ "phủ" kiểu lên control đã có (tìm theo tên), KHÔNG đổi tên/Items/Text/sự kiện => khớp code Form22 hiện tại.
    /// - Gọi MỘT LẦN trong constructor, sau Module_BieuTuongNut.ApDung(this):  Module_GiaoDienForm22Web.ApDung(this);
    /// - Đã tối ưu đa tỉ lệ DPI (100%, 125%, 150%...): không cắt chữ, không bể bố cục.
    /// </summary>
    internal static class Module_GiaoDienForm22Web
    {
        // ===== Bảng màu =====
        private static readonly Color Nen = Color.FromArgb(244, 247, 252);
        private static readonly Color The = Color.White;
        private static readonly Color Vien = Color.FromArgb(226, 232, 240);
        private static readonly Color ChuDam = Color.FromArgb(30, 41, 59);
        private static readonly Color ChuXam = Color.FromArgb(71, 85, 105);
        private static readonly Color ChuMo = Color.FromArgb(148, 163, 184);
        private static readonly Color Xanh = Color.FromArgb(37, 99, 235);
        private static readonly Color XanhDam = Color.FromArgb(29, 78, 216);
        private static readonly Color XanhNhat = Color.FromArgb(239, 246, 255);
        private static readonly Color Tim = Color.FromArgb(79, 70, 229);
        private static readonly Color HoPhach = Color.FromArgb(245, 158, 11);

        private static readonly Color LoaiTot = Color.FromArgb(22, 163, 74);
        private static readonly Color LoaiKha = Color.FromArgb(37, 99, 235);
        private static readonly Color LoaiTB = Color.FromArgb(217, 119, 6);
        private static readonly Color LoaiYeu = Color.FromArgb(220, 38, 38);

        // Font dùng chung (tạo 1 lần)
        private static readonly Font FTieuDeThe = new("Segoe UI Semibold", 10.5F);
        private static readonly Font FNhan = new("Segoe UI", 9.5F);
        private static readonly Font FOChon = new("Segoe UI", 9.5F);
        private static readonly Font FOChonDam = new("Segoe UI Semibold", 9.5F);
        private static readonly Font FTen = new("Segoe UI Semibold", 14.5F);
        private static readonly Font FNut = new("Segoe UI Semibold", 10F);
        private static readonly Font FTrangThai = new("Segoe UI", 9F);

        private const string CO_DA_AP = "__Form22Web";

        private static readonly PropertyInfo? _doubleBuffered =
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);

        // ================= ĐIỂM VÀO =================
        public static void ApDung(Form form)
        {
            if (form == null) return;
            if (form.Tag is string t && t == CO_DA_AP) return;   // chống áp trùng

            form.SuspendLayout();
            try
            {
                form.BackColor = Nen;
                BatDoubleBuffer(form);

                BoCucNgang(form);

                var tlpChinh = Tim_<TableLayoutPanel>(form, "tableLayoutPanel1");
                if (tlpChinh != null)
                {
                    tlpChinh.BackColor = Nen;
                    tlpChinh.Padding = new Padding(Dp(form, 12), Dp(form, 8), Dp(form, 12), Dp(form, 6));
                    BatDoubleBuffer(tlpChinh);
                }

                DinhDangHeader(form);
                DinhDangThe(Tim_<GroupBox>(form, "groupBox1_ThongTinNamCu"));
                DinhDangThe(Tim_<GroupBox>(form, "groupBox2_ThongTinThiDuaKhenThuongNamHienTai"));

                foreach (var n in new[] { "tableLayoutPanel3", "tableLayoutPanel4" })
                {
                    var p = Tim_<TableLayoutPanel>(form, n);
                    if (p == null) continue;
                    p.BackColor = The;
                    BatDoubleBuffer(p);
                }

                DuyetControl(form);                 // nhãn + combobox
                DinhDangNut(Tim_<KryptonButton>(form, "kryptonButton1_CapNhat"));

                var thanhTrangThai = Tim_<ToolStrip>(form, "toolStrip1");
                var nhanTrangThai = thanhTrangThai?.Items.Find("toolStripLabel1", false).FirstOrDefault() as ToolStripLabel;
                DinhDangTrangThai(thanhTrangThai, nhanTrangThai);

                form.Tag = CO_DA_AP;
            }
            finally { form.ResumeLayout(true); }
        }

        // ================= BỐ CỤC NGANG & CHỐNG BỂ DPI =================
        private static void BoCucNgang(Form form)
        {
            int wChuan = Dp(form, 1140);
            int hChuan = Dp(form, 560);
            int wMoi = Math.Max((int)(form.ClientSize.Width * 1.32), wChuan);
            int hMoi = Math.Max((int)(form.ClientSize.Height * 0.96), hChuan);

            form.MinimumSize = new Size(Dp(form, 1080), Dp(form, 530));
            form.ClientSize = new Size(wMoi, hMoi);

            var tlp1 = Tim_<TableLayoutPanel>(form, "tableLayoutPanel1");
            if (tlp1 != null && tlp1.RowStyles.Count >= 4)
            {
                tlp1.RowStyles[0].Height = 13F;   // header
                tlp1.RowStyles[1].Height = 17F;   // thẻ năm cũ (1 hàng ngang gọn)
                tlp1.RowStyles[2].Height = 58F;   // thẻ năm hiện tại (5 hàng tháng)
                tlp1.RowStyles[3].Height = 12F;   // nút
            }

            SapXepNamCuMotHang(form);
            SapXepNamHienTai(form);
        }

        /// <summary>
        /// Đưa 3 cặp (nhãn + combobox) của phần Năm cũ lên cùng 1 hàng ngang (6 cột x 1 hàng).
        /// </summary>
        private static void SapXepNamCuMotHang(Form form)
        {
            var tlp4 = Tim_<TableLayoutPanel>(form, "tableLayoutPanel4");
            if (tlp4 == null) return;

            var capNamCu = new List<(string nhan, string combo)>
            {
                ("label_KetQuaThiDuaNamCu", "comboBox_KetQuaThiDuaNamCu"),
                ("label_KetQuaXepLoaiCBCS", "comboBox_KetQuaXepLoaiCBCS"),
                ("label_KetQuaXepLoaiDang", "comboBox_KetQuaXepLoaiDang"),
            };

            var tatCaNhan = tlp4.Controls.OfType<Label>().ToList();
            var tatCaCombo = tlp4.Controls.OfType<ComboBox>().ToList();

            var danhSachCap = new List<(Label lb, ComboBox cb)>();
            foreach (var (tenLb, tenCb) in capNamCu)
            {
                var lb = Tim_<Label>(tlp4, tenLb);
                var cb = Tim_<ComboBox>(tlp4, tenCb);
                if (lb != null && cb != null)
                    danhSachCap.Add((lb, cb));
            }

            if (danhSachCap.Count < 3)
            {
                danhSachCap.Clear();
                var nhanLoc = tatCaNhan
                    .Where(x => x.Name != "label_ID_SoHieu" && !x.Text.Contains("Số hiệu", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.Top).ThenBy(x => x.Left)
                    .ToList();
                var comboLoc = tatCaCombo
                    .OrderBy(x => x.Top).ThenBy(x => x.Left)
                    .ToList();

                int n = Math.Min(nhanLoc.Count, comboLoc.Count);
                for (int i = 0; i < n; i++)
                    danhSachCap.Add((nhanLoc[i], comboLoc[i]));
            }

            if (danhSachCap.Count == 0) return;

            tlp4.SuspendLayout();
            try
            {
                int soCap = danhSachCap.Count;
                tlp4.ColumnCount = soCap * 2;
                tlp4.RowCount = 1;

                tlp4.ColumnStyles.Clear();
                tlp4.RowStyles.Clear();
                tlp4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                for (int i = 0; i < soCap; i++)
                {
                    var (lb, cb) = danhSachCap[i];
                    tlp4.SetCellPosition(lb, new TableLayoutPanelCellPosition(i * 2, 0));
                    tlp4.SetCellPosition(cb, new TableLayoutPanelCellPosition(i * 2 + 1, 0));

                    lb.AutoSize = true;
                    lb.Anchor = AnchorStyles.Left;
                    lb.Margin = new Padding(Dp(tlp4, 8), 0, Dp(tlp4, 4), 0);

                    cb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                    cb.Margin = new Padding(Dp(tlp4, 2), Dp(tlp4, 2), i == soCap - 1 ? Dp(tlp4, 8) : Dp(tlp4, 14), Dp(tlp4, 2));

                    tlp4.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                    tlp4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / soCap));
                }
            }
            finally { tlp4.ResumeLayout(true); }
        }

        private static void SapXepNamHienTai(Form form)
        {
            var tlp3 = Tim_<TableLayoutPanel>(form, "tableLayoutPanel3");
            if (tlp3 == null) return;

            var thuTu = new List<(string nhan, string combo)>
            {
                ("label_Thang_12_NamCu", "comboBox_KQPhanLoaiThang_12_NamCu"),
                ("label_Thang_1", "comboBox_KQPhanLoaiThang_1"),
                ("label_Thang_2", "comboBox_KQPhanLoaiThang_2"),
                ("label_Thang_3", "comboBox_KQPhanLoaiThang_3"),
                ("label_Thang_4", "comboBox_KQPhanLoaiThang_4"),
                ("label_Thang_5", "comboBox_KQPhanLoaiThang_5"),
                ("label4", "comboBox_KQPhanLoai6_Thang_Dau_Nam"),
                ("label_Thang_6", "comboBox_KQPhanLoaiThang_6"),
                ("label_Thang_7", "comboBox_KQPhanLoaiThang_7"),
                ("label_Thang_8", "comboBox_KQPhanLoaiThang_8"),
                ("label_Thang_9", "comboBox_KQPhanLoaiThang_9"),
                ("label_Thang_10", "comboBox_KQPhanLoaiThang_10"),
                ("label_Thang_11", "comboBox_KQPhanLoaiThang_11"),
                ("label6", "comboBox_KQPhanLoaiTongKet_Nam"),
            };

            tlp3.SuspendLayout();
            try
            {
                tlp3.ColumnCount = 6;

                for (int k = 0; k < thuTu.Count; k++)
                {
                    var lb = Tim_<Label>(form, thuTu[k].nhan);
                    var cb = Tim_<ComboBox>(form, thuTu[k].combo);
                    if (lb == null || cb == null) continue;

                    int cot = (k / 5) * 2, hang = k % 5;
                    tlp3.SetCellPosition(lb, new TableLayoutPanelCellPosition(cot, hang));
                    tlp3.SetCellPosition(cb, new TableLayoutPanelCellPosition(cot + 1, hang));

                    lb.AutoSize = true;
                    lb.Anchor = AnchorStyles.Left;
                    lb.Margin = new Padding(Dp(tlp3, 10), 0, Dp(tlp3, 6), 0);

                    cb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                    cb.Margin = new Padding(Dp(tlp3, 2), Dp(tlp3, 3), Dp(tlp3, 20), Dp(tlp3, 3));
                }

                tlp3.RowCount = 5;
                tlp3.ColumnStyles.Clear();
                tlp3.RowStyles.Clear();
                for (int i = 0; i < 3; i++)
                {
                    tlp3.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                    tlp3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
                }
                for (int i = 0; i < 5; i++)
                    tlp3.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            }
            finally { tlp3.ResumeLayout(true); }
        }

        // ================= HEADER (SỐ HIỆU BÊN PHẢI HỌ TÊN, KHÔNG NỀN, CÙNG FONT & CỠ CHỮ) =================
        private static void DinhDangHeader(Form form)
        {
            var tlp = Tim_<TableLayoutPanel>(form, "tableLayoutPanel2");
            if (tlp == null) return;

            tlp.BackColor = Nen;
            tlp.Margin = new Padding(0, 0, 0, Dp(tlp, 6));
            tlp.Padding = new Padding(Dp(tlp, 12), Dp(tlp, 4), Dp(tlp, 16), Dp(tlp, 4));
            BatDoubleBuffer(tlp);
            tlp.Paint += VeHeader;
            tlp.Resize += (_, _) => tlp.Invalidate();

            var pic = Tim_<PictureBox>(form, "pictureBox1");
            if (pic != null)
            {
                pic.BackColor = Color.Transparent;
                pic.SizeMode = PictureBoxSizeMode.Zoom;
            }

            var ten = Tim_<Label>(form, "label1_ID_HoVaTen");
            if (ten != null)
            {
                ten.BackColor = Color.Transparent;
                ten.ForeColor = Color.White;
                ten.Font = FTen;
            }

            var lbSoHieuGoc = Tim_<Label>(form, "label_ID_SoHieu");
            if (lbSoHieuGoc != null)
            {
                if (lbSoHieuGoc.Parent is TableLayoutPanel tlpCha)
                {
                    var pos = tlpCha.GetPositionFromControl(lbSoHieuGoc);
                    if (pos.Column > 0 && tlpCha.GetControlFromPosition(pos.Column - 1, pos.Row) is Label lbTieuDe)
                        lbTieuDe.Visible = false;
                }
                lbSoHieuGoc.Visible = false;

                var nhanSoHieuHeader = new Label
                {
                    Name = "__soHieuHeader",
                    AutoSize = true,
                    Font = FTen,                     // cùng font và cỡ chữ với Họ và tên
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,   // không tô màu nền, chỉ để chữ
                    Padding = Padding.Empty,
                    Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                    TextAlign = ContentAlignment.MiddleRight,
                    Margin = new Padding(Dp(tlp, 12), 0, 0, 0)
                };

                void CapNhatTextSoHieu()
                {
                    string sh = (lbSoHieuGoc.Text ?? "").Trim();
                    nhanSoHieuHeader.Text = string.IsNullOrEmpty(sh) ? "" : $"Số hiệu CAND: {sh}";
                    nhanSoHieuHeader.Visible = !string.IsNullOrEmpty(sh);
                }

                CapNhatTextSoHieu();
                lbSoHieuGoc.TextChanged += (_, _) => CapNhatTextSoHieu();

                if (tlp.ColumnCount < 3)
                {
                    tlp.ColumnCount = 3;
                    tlp.ColumnStyles.Clear();
                    tlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));        // Logo
                    tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));   // Họ và tên
                    tlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));        // Số hiệu CAND
                }
                tlp.Controls.Add(nhanSoHieuHeader, tlp.ColumnCount - 1, 0);
            }
        }

        private static void VeHeader(object? sender, PaintEventArgs e)
        {
            if (sender is not Control c || c.Width < 8 || c.Height < 8) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var r = new Rectangle(0, 0, c.Width - 1, c.Height - 1);
            using var path = BoGoc(r, Dp(c, 12));
            using (var br = new LinearGradientBrush(r, Xanh, Tim, 20F))
                g.FillPath(br, path);

            var state = g.Save();
            g.SetClip(path, CombineMode.Intersect);
            using (var b1 = new SolidBrush(Color.FromArgb(28, 255, 255, 255)))
            using (var b2 = new SolidBrush(Color.FromArgb(18, 255, 255, 255)))
            {
                g.FillEllipse(b1, r.Right - Dp(c, 150), -Dp(c, 60), Dp(c, 220), Dp(c, 220));
                g.FillEllipse(b2, r.Right - Dp(c, 60), r.Bottom - Dp(c, 40), Dp(c, 130), Dp(c, 130));
            }
            g.Restore(state);
        }

        // ================= THẺ =================
        private static void DinhDangThe(GroupBox? gb)
        {
            if (gb == null) return;
            gb.BackColor = The;
            gb.ForeColor = ChuDam;
            gb.Font = FTieuDeThe;
            gb.Margin = new Padding(0, Dp(gb, 3), 0, Dp(gb, 3));
            gb.Padding = new Padding(Dp(gb, 10), Dp(gb, 14), Dp(gb, 10), Dp(gb, 6));
            BatDoubleBuffer(gb);
            gb.Paint += VeThe;
            gb.Resize += (_, _) => gb.Invalidate();
        }

        private static void VeThe(object? sender, PaintEventArgs e)
        {
            if (sender is not GroupBox gb || gb.Width < 8 || gb.Height < 8) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.Clear(Nen);
            var r = new Rectangle(0, 0, gb.Width - 1, gb.Height - 1);
            using var path = BoGoc(r, Dp(gb, 10));
            using (var fill = new SolidBrush(The)) g.FillPath(fill, path);
            using (var pen = new Pen(Vien)) g.DrawPath(pen, path);

            var mau = gb.Name.Contains("NamCu", StringComparison.OrdinalIgnoreCase) ? HoPhach : Xanh;
            using (var bar = new SolidBrush(mau))
                g.FillRectangle(bar, Dp(gb, 14), Dp(gb, 8), Dp(gb, 4), Dp(gb, 16));

            var vung = new Rectangle(Dp(gb, 24), Dp(gb, 4), gb.Width - Dp(gb, 36), Dp(gb, 24));
            TextRenderer.DrawText(g, gb.Text, FTieuDeThe, vung, ChuDam,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        // ================= NHÃN + COMBOBOX =================
        private static void DuyetControl(Control cha)
        {
            foreach (Control c in cha.Controls)
            {
                switch (c)
                {
                    case Label lb when lb.Name != "label1_ID_HoVaTen"
                                    && lb.Name != "label_ID_SoHieu"
                                    && lb.Name != "__soHieuHeader":
                        lb.Font = FNhan;
                        lb.ForeColor = ChuXam;
                        break;
                    case ComboBox cb:
                        DinhDangCombo(cb);
                        break;
                }
                if (c.HasChildren) DuyetControl(c);
            }
        }

        private static void DinhDangCombo(ComboBox cb)
        {
            cb.FlatStyle = FlatStyle.Flat;
            cb.BackColor = Color.White;
            cb.ForeColor = ChuDam;
            cb.Font = FOChon;
            cb.DrawMode = DrawMode.OwnerDrawFixed;
            cb.ItemHeight = cb.Font.Height + 6;
            cb.DrawItem -= CbVeMuc;
            cb.DrawItem += CbVeMuc;
            CbSon.Gan(cb);
        }

        /// <summary>Vẽ từng mục trong danh sách thả xuống với khung bo tròn khi chọn.</summary>
        private static void CbVeMuc(object? sender, DrawItemEventArgs e)
        {
            if (sender is not ComboBox cb) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var nenTrang = new SolidBrush(Color.White))
                g.FillRectangle(nenTrang, e.Bounds);

            if (e.Index < 0 || e.Index >= cb.Items.Count) return;

            bool dangChon = (e.State & DrawItemState.Selected) != 0;
            var khungItem = e.Bounds;
            khungItem.Inflate(-Dp(cb, 4), -Dp(cb, 1));

            if (dangChon && khungItem.Width > 4 && khungItem.Height > 4)
            {
                using var path = BoGoc(khungItem, Dp(cb, 5));
                using var bChon = new SolidBrush(Color.FromArgb(224, 236, 255));
                using var pVien = new Pen(Color.FromArgb(147, 197, 253));
                g.FillPath(bChon, path);
                g.DrawPath(pVien, path);
            }

            string text = cb.Items[e.Index]?.ToString() ?? "";
            VeNhanXepLoai(g, text, e.Bounds, true, khungItem.Left + Dp(cb, 8));
        }

        /// <summary>Chỉ vẽ chữ đậm theo màu xếp loại; ô trống hiện "—".</summary>
        private static void VeNhanXepLoai(Graphics g, string text, Rectangle vung, bool enabled, int x)
        {
            if (text.Length == 0)
            {
                TextRenderer.DrawText(g, "—", FOChon,
                    new Point(x, vung.Top + (vung.Height - FOChon.Height) / 2), ChuMo, TextFormatFlags.NoPadding);
                return;
            }

            Color mau = MauXepLoai(text);
            var vungChu = new Rectangle(x, vung.Top, Math.Max(10, vung.Right - x), vung.Height);
            TextRenderer.DrawText(g, text, FOChonDam, vungChu,
                enabled ? mau : ChuXam,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        private static Color MauXepLoai(string v) => v.Trim().ToUpperInvariant() switch
        {
            "HTXSNV" or "CSTĐ" or "CSTD" or "1" => LoaiTot,
            "HTTNV" or "CSTT" or "2" => LoaiKha,
            "HTNV" or "3" => LoaiTB,
            "KHTNV" or "4" => LoaiYeu,
            _ => ChuDam
        };

        // ================= COMBOBOX TỰ VẼ + POPUP BO TRÒN =================
        private sealed class CbSon : NativeWindow
        {
            private const int WM_PAINT = 0x000F;
            private const int WM_ERASEBKGND = 0x0014;

            private delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

            [DllImport("user32.dll")]
            private static extern bool ValidateRect(IntPtr hWnd, IntPtr lpRect);

            [DllImport("user32.dll")]
            private static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

            [DllImport("user32.dll")]
            private static extern bool IsWindowVisible(IntPtr hWnd);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

            [DllImport("user32.dll")]
            private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

            [DllImport("user32.dll")]
            private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

            [DllImport("user32.dll")]
            private static extern IntPtr GetWindowDC(IntPtr hWnd);

            [DllImport("user32.dll")]
            private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

            [DllImport("gdi32.dll")]
            private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

            [StructLayout(LayoutKind.Sequential)]
            private struct RECT { public int Left, Top, Right, Bottom; }

            private readonly ComboBox _cb;
            private bool _hover;

            public static void Gan(ComboBox cb) => _ = new CbSon(cb);

            private CbSon(ComboBox cb)
            {
                _cb = cb;
                cb.HandleCreated += (_, _) => { if (Handle == IntPtr.Zero) AssignHandle(cb.Handle); };
                cb.HandleDestroyed += (_, _) => { if (Handle != IntPtr.Zero) ReleaseHandle(); };
                cb.MouseEnter += (_, _) => { _hover = true; cb.Invalidate(); };
                cb.MouseLeave += (_, _) => { _hover = false; cb.Invalidate(); };
                cb.GotFocus += (_, _) => cb.Invalidate();
                cb.LostFocus += (_, _) => cb.Invalidate();
                cb.DropDown += (_, _) => { cb.BeginInvoke(BoTronDanhSachSoRa); cb.Invalidate(); };
                cb.DropDownClosed += (_, _) => cb.Invalidate();
                cb.EnabledChanged += (_, _) => cb.Invalidate();
                cb.SelectedIndexChanged += (_, _) => cb.Invalidate();
                cb.TextChanged += (_, _) => cb.Invalidate();
                if (cb.IsHandleCreated) AssignHandle(cb.Handle);
            }

            private void BoTronDanhSachSoRa()
            {
                if (!_cb.DroppedDown || _cb.IsDisposed) return;

                IntPtr popup = IntPtr.Zero;
                var sb = new StringBuilder(64);
                uint threadId = (uint)Thread.CurrentThread.ManagedThreadId;
                EnumThreadWindows(threadId, (hWnd, _) =>
                {
                    if (!IsWindowVisible(hWnd)) return true;
                    sb.Clear();
                    GetClassName(hWnd, sb, sb.Capacity);
                    if (sb.ToString().Equals("ComboLBox", StringComparison.OrdinalIgnoreCase))
                    {
                        popup = hWnd;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);

                if (popup != IntPtr.Zero && GetWindowRect(popup, out RECT rc))
                {
                    int w = rc.Right - rc.Left;
                    int h = rc.Bottom - rc.Top;
                    int ban = Math.Max(6, Dp(_cb, 8));
                    if (w > 8 && h > 8)
                    {
                        IntPtr rgn = CreateRoundRectRgn(0, 0, w, h, ban * 2, ban * 2);
                        if (rgn != IntPtr.Zero)
                            SetWindowRgn(popup, rgn, true);

                        // Vẽ viền bo tròn đồng bộ lên khung popup
                        IntPtr hdc = GetWindowDC(popup);
                        if (hdc != IntPtr.Zero)
                        {
                            try
                            {
                                using var g = Graphics.FromHdc(hdc);
                                g.SmoothingMode = SmoothingMode.AntiAlias;
                                var rectVien = new Rectangle(1, 1, w - 3, h - 3);
                                using var path = BoGoc(rectVien, ban);
                                using var pen = new Pen(Xanh, Math.Max(1.2F, Dp(_cb, 1)));
                                g.DrawPath(pen, path);
                            }
                            finally { ReleaseDC(popup, hdc); }
                        }
                    }
                }
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_ERASEBKGND) { m.Result = (IntPtr)1; return; }
                if (m.Msg == WM_PAINT)
                {
                    try { Ve(); }
                    catch { base.WndProc(ref m); return; }
                    ValidateRect(Handle, IntPtr.Zero);
                    m.Result = IntPtr.Zero;
                    return;
                }
                base.WndProc(ref m);
            }

            private void Ve()
            {
                var r = _cb.ClientRectangle;
                if (r.Width < 6 || r.Height < 6) return;

                bool en = _cb.Enabled, dropped = _cb.DroppedDown, focus = _cb.Focused;
                using var g0 = Graphics.FromHwnd(Handle);
                using var buf = BufferedGraphicsManager.Current.Allocate(g0, r);
                var g = buf.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(The);

                Color nen = !en ? Color.FromArgb(241, 245, 249) : (_hover || dropped) ? XanhNhat : Color.White;
                Color vien = (dropped || focus) && en ? Xanh : _hover && en ? Color.FromArgb(147, 197, 253) : Color.FromArgb(203, 213, 225);

                var rr = new Rectangle(0, 0, r.Width - 1, r.Height - 1);
                using (var path = BoGoc(rr, Dp(_cb, 6)))
                {
                    using (var fill = new SolidBrush(nen)) g.FillPath(fill, path);
                    using (var pen = new Pen(vien)) g.DrawPath(pen, path);
                }

                string text = _cb.SelectedItem?.ToString() ?? _cb.Text ?? "";
                VeNhanXepLoai(g, text, new Rectangle(0, 0, r.Width - Dp(_cb, 22), r.Height), en, Dp(_cb, 10));

                int cx = r.Width - Dp(_cb, 13), cy = r.Height / 2, w = Dp(_cb, 4), h = Dp(_cb, 2);
                using (var pen = new Pen(en ? ChuXam : ChuMo, Math.Max(1.6F, Dp(_cb, 2) * 0.9F)))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    g.DrawLines(pen, new[]
                    {
                        new Point(cx - w, cy - h),
                        new Point(cx, cy + h),
                        new Point(cx + w, cy - h)
                    });
                }

                buf.Render();
            }
        }

        // ================= NÚT BẤM =================
        private static void DinhDangNut(KryptonButton? kb)
        {
            if (kb == null) return;

            DatMauNut(kb.StateCommon, Xanh);
            DatMauNut(kb.StateNormal, Xanh);
            DatMauNut(kb.OverrideDefault, Xanh);
            DatMauNut(kb.OverrideFocus, Xanh);

            DatMauNut(kb.StateTracking, XanhDam);
            DatMauNut(kb.StatePressed, Color.FromArgb(30, 64, 175));

            kb.StateCommon.Border.Rounding = 8F;
            kb.StateCommon.Content.ShortText.Font = FNut;
            kb.Cursor = Cursors.Hand;
        }

        private static void DatMauNut(PaletteTriple? s, Color nen)
        {
            if (s == null) return;
            ApDungMau(s.Back, s.Border, s.Content, nen);
        }

        private static void DatMauNut(PaletteTripleRedirect? s, Color nen)
        {
            if (s == null) return;
            ApDungMau(s.Back, s.Border, s.Content, nen);
        }

        private static void ApDungMau(PaletteBack back, PaletteBorder border, PaletteContent content, Color nen)
        {
            back.ColorStyle = PaletteColorStyle.Solid;
            back.Color1 = nen;
            back.Color2 = nen;
            border.Color1 = nen;
            border.Color2 = nen;
            content.ShortText.Color1 = Color.White;
            content.ShortText.Color2 = Color.White;
        }

        // ================= THANH TRẠNG THÁI =================
        private static void DinhDangTrangThai(ToolStrip? ts, ToolStripLabel? lb)
        {
            if (ts == null) return;
            ts.GripStyle = ToolStripGripStyle.Hidden;
            ts.BackColor = Color.FromArgb(238, 242, 248);
            ts.Renderer = new ToolStripProfessionalRenderer(new BangMauPhang()) { RoundedEdges = false };
            if (lb != null)
            {
                lb.Font = FTrangThai;
                lb.ForeColor = ChuXam;
            }
        }

        private sealed class BangMauPhang : ProfessionalColorTable
        {
            public BangMauPhang() { UseSystemColors = false; }
            public override Color ToolStripGradientBegin => Color.FromArgb(238, 242, 248);
            public override Color ToolStripGradientMiddle => Color.FromArgb(238, 242, 248);
            public override Color ToolStripGradientEnd => Color.FromArgb(238, 242, 248);
            public override Color ToolStripBorder => Vien;
        }

        // ================= TIỆN ÍCH =================
        private static T? Tim_<T>(Control goc, string ten) where T : Control
        {
            var kq = goc.Controls.Find(ten, true);
            return kq.Length > 0 ? kq[0] as T : null;
        }

        private static void BatDoubleBuffer(Control c)
        {
            try { _doubleBuffered?.SetValue(c, true); } catch { /* bỏ qua */ }
        }

        private static int Dp(Control c, int v) => (int)Math.Round(v * c.DeviceDpi / 96.0);

        private static GraphicsPath BoGoc(Rectangle r, int ban)
        {
            int d = Math.Max(2, ban * 2);
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}