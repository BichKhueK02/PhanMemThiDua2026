using Krypton.Toolkit;
using System.Diagnostics;
using System.Drawing.Drawing2D;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Giao diện kiểu web cho Form33 (Kiểm tra sức khỏe CSDL) - CHỈ trình bày, không đụng logic.
    /// Gọi 1 dòng cuối constructor:   GiaoDienForm33Web.ApDung(this);
    /// Phần bảng: DinhDangLuoi (kiểu dáng, gọi trong ApDung) + DatCot (cỡ cột, gọi sau khi gán DataSource).
    /// Mỗi phần có try/catch riêng: lỗi ở một phần không làm hỏng phần còn lại (log: Debug output).
    /// </summary>
    internal static class GiaoDienForm33Web
    {
        // ===== Bảng màu (đồng bộ với GiaoDienForm12Web) =====
        public static readonly Color MauNen = Color.FromArgb(250, 250, 252);
        public static readonly Color MauChu = Color.FromArgb(31, 41, 55);
        public static readonly Color MauChuPhu = Color.FromArgb(75, 85, 99);
        public static readonly Color MauVien = Color.FromArgb(229, 231, 235);
        public static readonly Color MauOk = Color.FromArgb(21, 128, 61);
        public static readonly Color MauCanhBao = Color.FromArgb(194, 65, 12);
        public static readonly Color MauLoi = Color.FromArgb(185, 28, 28);
        private static readonly Color MauDauCot = Color.FromArgb(249, 250, 251);
        private static readonly Color MauChonNen = Color.FromArgb(219, 234, 254);
        private static readonly Color MauChonChu = Color.FromArgb(30, 58, 138);

        /// <summary>Màu hàng theo mức: 2 = lỗi, 1 = cảnh báo, 0 = tốt, còn lại = đang chờ (trung tính).</summary>
        public static (Color Nen, Color Chu) MauTheoMuc(int muc) => muc switch
        {
            2 => (Color.FromArgb(254, 226, 226), Color.FromArgb(127, 29, 29)),
            1 => (Color.FromArgb(254, 243, 199), Color.FromArgb(120, 53, 15)),
            0 => (Color.FromArgb(236, 253, 245), Color.FromArgb(20, 83, 45)),
            _ => (Color.White, MauChuPhu),
        };

        private static readonly Dictionary<(string, float, FontStyle), Font> _fonts = new();
        private static Font LayFont(float co, FontStyle kieu)
        {
            lock (_fonts)
            {
                var k = (Module_HeThong.TenFontHeThong, co, kieu);
                if (!_fonts.TryGetValue(k, out var f))
                    _fonts[k] = f = new Font(k.Item1, co, kieu, GraphicsUnit.Point);
                return f;
            }
        }

        public static void ApDung(Form? f)
        {
            if (f == null) return;
            Chay("nen", () => ChinhNen(f));
            Chay("the dau trang", () => ChinhTheDauTrang(f));
            Chay("khung", () => ChinhKhung(f));
            Chay("thanh trang thai", () => ChinhThanhTrangThai(f));
            Chay("nut", () => ChinhNut(f));
            Chay("luoi", () =>
            {
                foreach (var g in Tim<DataGridView>(f)) DinhDangLuoi(g);
            });
        }

        private static void Chay(string ten, Action a)
        {
            try { a(); }
            catch (Exception ex) { Debug.WriteLine("[GiaoDienForm33Web] " + ten + ": " + ex.Message); }
        }

        private static IEnumerable<T> Tim<T>(Control goc) where T : Control
        {
            foreach (Control c in goc.Controls)
            {
                if (c is T t) yield return t;
                foreach (var con in Tim<T>(c)) yield return con;
            }
        }
        private static T? Tim<T>(Control goc, string ten) where T : Control
            => goc.Controls.Find(ten, true).OfType<T>().FirstOrDefault();

        // ------------------------------------------------------------------
        private static void ChinhNen(Form f)
        {
            f.BackColor = MauNen;
            var t1 = Tim<TableLayoutPanel>(f, "tableLayoutPanel1");
            if (t1 != null)
            {
                t1.BackColor = MauNen;
                t1.Padding = new Padding(10, 6, 10, 4);
            }
        }

        // Thẻ trắng bo góc chứa người khai thác / phiên bản / 3 nút
        private static void ChinhTheDauTrang(Form f)
        {
            var the = Tim<TableLayoutPanel>(f, "tableLayoutPanel2");
            if (the != null)
            {
                the.BackColor = Color.White;
                the.Paint += (s, e) =>
                {
                    var c = (Control)s!;
                    if (c.Width < 4 || c.Height < 4) return;
                    var g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using var pen = new Pen(MauVien);
                    using var path = HinhBoTron(new Rectangle(0, 0, c.Width - 1, c.Height - 1), 8);
                    g.DrawPath(pen, path);
                };
            }
            foreach (var ten in new[] { "tableLayoutPanel3", "tableLayoutPanel4" })
            {
                var p = Tim<TableLayoutPanel>(f, ten);
                if (p != null) p.BackColor = Color.White;
            }
            foreach (var ten in new[] { "label_TenAdminDangNhap", "lable_phienban" })
            {
                var lb = Tim<Label>(f, ten);
                if (lb == null) continue;
                lb.Font = LayFont(9.5f, FontStyle.Regular);
                lb.ForeColor = MauChuPhu;
            }
        }

        private static GraphicsPath HinhBoTron(Rectangle r, int bk)
        {
            int d = bk * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private static void ChinhKhung(Form f)
        {
            foreach (var gb in Tim<GroupBox>(f))
            {
                gb.BackColor = Color.White;
                gb.ForeColor = MauChu;
                gb.Font = LayFont(10.5f, FontStyle.Bold);
            }
        }

        private static void ChinhThanhTrangThai(Form f)
        {
            var ss = Tim<StatusStrip>(f, "statusStrip1");
            if (ss == null) return;
            ss.BackColor = Color.FromArgb(243, 244, 246);
            ss.ForeColor = MauChuPhu;
            ss.SizingGrip = false;
        }

        private static void ChinhNut(Form f)
        {
            foreach (var b in Tim<KryptonButton>(f))
            {
                // "Làm mới" là nút chính (xanh đặc); các nút còn lại xanh nhạt
                bool chinh = b.Name.Contains("CapNhat", StringComparison.OrdinalIgnoreCase);
                Module_GiaoDienForm12Web.DatKieuNut(b, chinh);
            }
        }

        // ------------------------------------------------------------------
        //  BẢNG
        // ------------------------------------------------------------------
        /// <summary>Kiểu dáng bảng (idempotent). Gọi được trước khi có cột.</summary>
        public static void DinhDangLuoi(DataGridView dgv)
        {
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToOrderColumns = false;
            dgv.AllowUserToResizeRows = false;
            dgv.RowHeadersVisible = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = Color.FromArgb(240, 241, 244);
            dgv.ScrollBars = ScrollBars.Both;

            var hs = dgv.ColumnHeadersDefaultCellStyle;
            hs.Alignment = DataGridViewContentAlignment.MiddleCenter;
            hs.BackColor = MauDauCot;
            hs.ForeColor = MauChuPhu;
            hs.SelectionBackColor = MauDauCot;
            hs.SelectionForeColor = MauChuPhu;
            hs.Font = LayFont(9.5f, FontStyle.Bold);
            hs.WrapMode = DataGridViewTriState.True;
            hs.Padding = new Padding(4, 0, 4, 0);
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 42;

            var ds = dgv.DefaultCellStyle;
            ds.Font = LayFont(9.5f, FontStyle.Regular);
            ds.BackColor = Color.White;
            ds.ForeColor = MauChu;
            ds.SelectionBackColor = MauChonNen;
            ds.SelectionForeColor = MauChonChu;
            ds.Padding = new Padding(6, 0, 6, 0);
            ds.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            dgv.RowTemplate.Height = 38;

            if (dgv is KryptonDataGridView k)
            {
                try
                {
                    k.GridStyles.Style = DataGridViewStyle.List;
                    k.StateCommon.Background.Color1 = Color.White;
                    k.StateCommon.HeaderColumn.Back.ColorStyle = PaletteColorStyle.Solid;
                    k.StateCommon.HeaderColumn.Back.Color1 = MauDauCot;
                    k.StateCommon.HeaderColumn.Back.Color2 = MauDauCot;
                    k.StateCommon.HeaderColumn.Border.Color1 = MauVien;
                    k.StateCommon.HeaderColumn.Border.Color2 = MauVien;
                    k.StateCommon.HeaderColumn.Border.DrawBorders = PaletteDrawBorders.Bottom;
                    k.StateCommon.HeaderColumn.Content.Color1 = MauChuPhu;
                    k.StateCommon.HeaderColumn.Content.Font = LayFont(9.5f, FontStyle.Bold);
                    k.StateCommon.DataCell.Content.Font = LayFont(9.5f, FontStyle.Regular);
                    k.StateSelected.DataCell.Back.Color1 = MauChonNen;
                    k.StateSelected.DataCell.Content.Color1 = MauChonChu;
                }
                catch (Exception ex) { Debug.WriteLine("[GiaoDienForm33Web] krypton luoi: " + ex.Message); }
            }
        }

        private static readonly Dictionary<string, float> _trongSoCot = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Trạng thái"] = 90,
            ["Thời gian tạo"] = 135,
            ["Dung lượng"] = 95,
            ["Sức khỏe"] = 175,
            ["Tốc độ phản hồi"] = 105,
            ["Mức độ phân mảnh"] = 150,
            ["Journal/WA"] = 80,
            ["Đọc"] = 55,
            ["Ghi"] = 55,
            ["Quyền đọc"] = 80,
            ["Quyền ghi"] = 80,
            ["Ổ đĩa"] = 80,
            ["Ổ đĩa trống"] = 100,
        };
        private static readonly HashSet<string> _cotCanGiua = new(StringComparer.OrdinalIgnoreCase)
        {
            "STT", "Đọc", "Ghi", "Quyền đọc", "Quyền ghi", "Ổ đĩa",
        };

        /// <summary>Cỡ và căn lề cột. Gọi MỘT LẦN sau khi gán DataSource (cột đã có).</summary>
        public static void DatCot(DataGridView dgv)
        {
            if (dgv.Columns.Count == 0) return;
            bool bangCSDL = dgv.Columns.Count > 9;
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
                if (col.Name == "STT")
                {
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    col.Width = 56;
                    col.MinimumWidth = 56;
                }
                else
                {
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    col.MinimumWidth = 48;
                    if (col.Name == "Đơn vị") col.FillWeight = bangCSDL ? 160 : 190;
                    else col.FillWeight = _trongSoCot.TryGetValue(col.Name, out var w) ? w : 100;
                }
                if (_cotCanGiua.Contains(col.Name))
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }
    }
}