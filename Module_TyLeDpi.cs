using System.Diagnostics;
using Krypton.Toolkit;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Hiệu chỉnh DPI cho các giá trị kích thước mà module giao diện web đặt bằng px CỐ ĐỊNH (chưa nhân tỷ lệ màn hình).
    /// Vấn đề: Windows co giãn mọi kích thước trong Designer ở cuối InitializeComponent (125% => x1,25); module giao diện chạy SAU đó
    /// và đặt lại vd. "cao nút tối đa 32", "hàng 48" theo px 100% => trên máy 125%/150% chữ to lên nhưng nút/hàng vẫn 32/48 px: chữ bị cắt.
    /// Cách làm (không phải sửa từng module):
    ///  1) Chup(f): ngay sau InitializeComponent (trước module giao diện) chụp kích thước nút, Min/MaxSize, hàng/cột Absolute của
    ///     TableLayoutPanel, chiều cao dòng/tiêu đề lưới, ClientSize của form.
    ///  2) HieuChinh(f, chup) lúc Load: giá trị nào KHÁC bản chụp => do module đặt bằng px 100% => nhân tỷ lệ DPI.
    ///     Giá trị không đổi (đã được Windows co giãn) giữ nguyên => không nhân 2 lần. Ở 100% (96 DPI) không làm gì.
    /// Chỉ dùng cho form có module KHÔNG tự nhân DPI (danh sách trong Module_BieuTuongNut).
    /// </summary>
    internal static class Module_TyLeDpi
    {
        internal sealed class BanChup
        {
            public readonly Dictionary<TableLayoutPanel, (List<(SizeType, float)> Hang, List<(SizeType, float)> Cot)> Bang = new();
            public readonly Dictionary<Control, (Size Co, Size Min, Size Max)> Nut = new();
            public readonly Dictionary<DataGridView, (int Dong, int TieuDe)> Luoi = new();
            public readonly Dictionary<SplitContainer, int> Chia = new();
            public Size ClientSize;
        }
        public static BanChup Chup(Form f)
        {
            var c = new BanChup { ClientSize = f.ClientSize };
            void Duyet(Control goc)
            {
                foreach (Control x in goc.Controls)
                {
                    switch (x)
                    {
                        case TableLayoutPanel t:
                            c.Bang[t] = (t.RowStyles.Cast<RowStyle>().Select(r => (r.SizeType, r.Height)).ToList(),
                                         t.ColumnStyles.Cast<ColumnStyle>().Select(r => (r.SizeType, r.Width)).ToList());
                            break;
                        case KryptonButton or ButtonBase:
                            c.Nut[x] = (x.Size, x.MinimumSize, x.MaximumSize);
                            break;
                        case DataGridView d:
                            c.Luoi[d] = (d.RowTemplate.Height, d.ColumnHeadersHeight);
                            break;
                        case SplitContainer sc:
                            c.Chia[sc] = sc.SplitterDistance;
                            break;
                    }
                    if (x.HasChildren) Duyet(x);
                }
            }
            Duyet(f);
            return c;
        }
        /// <summary>CHỈ để kiểm thử trên máy 100%: giả lập tỷ lệ DPI (null = dùng DPI thật).</summary>
        internal static float? TyLeGiaLap;
        public static void HieuChinh(Form f, BanChup c)
        {
            float k = TyLeGiaLap ?? f.DeviceDpi / 96f;
            if (k < 1.05f) return;                       // 100%: không cần
            int S(int v) => v <= 0 ? v : (int)Math.Round(v * k);
            float SF(float v) => v * k;
            f.SuspendLayout();
            try
            {
                // Form tự đặt cỡ cửa sổ bằng px 100% (vd. hộp thoại 760x560)
                if (f.TopLevel && f.ClientSize != c.ClientSize && f.WindowState == FormWindowState.Normal)
                {
                    var wa = Screen.FromControl(f).WorkingArea;
                    f.ClientSize = new Size(Math.Min(S(f.ClientSize.Width), wa.Width - 40), Math.Min(S(f.ClientSize.Height), wa.Height - 60));
                }
                foreach (var (t, (hangCu, cotCu)) in c.Bang)
                {
                    if (t.IsDisposed) continue;
                    t.SuspendLayout();
                    try
                    {
                        bool hangMoi = t.RowStyles.Count != hangCu.Count;   // module dựng lại toàn bộ kiểu hàng => mọi Absolute là px 100%
                        for (int i = 0; i < t.RowStyles.Count; i++)
                        {
                            var r = t.RowStyles[i];
                            if (r.SizeType != SizeType.Absolute) continue;
                            if (hangMoi || hangCu[i].Item1 != SizeType.Absolute || Math.Abs(hangCu[i].Item2 - r.Height) > 0.5f) r.Height = SF(r.Height);
                        }
                        bool cotMoi = t.ColumnStyles.Count != cotCu.Count;
                        for (int i = 0; i < t.ColumnStyles.Count; i++)
                        {
                            var col = t.ColumnStyles[i];
                            if (col.SizeType != SizeType.Absolute) continue;
                            if (cotMoi || cotCu[i].Item1 != SizeType.Absolute || Math.Abs(cotCu[i].Item2 - col.Width) > 0.5f) col.Width = SF(col.Width);
                        }
                    }
                    finally { t.ResumeLayout(false); }
                }
                foreach (var (x, (co, min, max)) in c.Nut)
                {
                    if (x.IsDisposed) continue;
                    if (x.MinimumSize != min) x.MinimumSize = new Size(S(x.MinimumSize.Width), S(x.MinimumSize.Height));
                    if (x.MaximumSize != max) x.MaximumSize = new Size(S(x.MaximumSize.Width), S(x.MaximumSize.Height));
                    if (x.Dock == DockStyle.None && !x.AutoSize)   // AutoSize: tự co theo chữ (đã đúng DPI) => không nhân
                    {
                        bool keoNgang = (x.Anchor & (AnchorStyles.Left | AnchorStyles.Right)) == (AnchorStyles.Left | AnchorStyles.Right);
                        bool keoDoc = (x.Anchor & (AnchorStyles.Top | AnchorStyles.Bottom)) == (AnchorStyles.Top | AnchorStyles.Bottom);
                        int w = x.Width, h = x.Height;
                        if (!keoNgang && w != co.Width) w = S(w);
                        if (!keoDoc && h != co.Height) h = S(h);
                        if (w != x.Width || h != x.Height) x.Size = new Size(w, h);
                    }
                }
                // Chiều cao hàng thẻ phía trên (khung chia NGANG, Panel1 cố định) do module đặt bằng px 100%.
                // Khung chia DỌC thường được tính từ bề rộng thật (đã đúng px) => không nhân.
                foreach (var (sc, cu) in c.Chia)
                {
                    if (sc.IsDisposed || sc.Orientation != Orientation.Horizontal || sc.SplitterDistance == cu) continue;
                    try { sc.SplitterDistance = Math.Min(S(sc.SplitterDistance), Math.Max(sc.Panel1MinSize, sc.Height - sc.Panel2MinSize - sc.SplitterWidth)); }
                    catch (ArgumentException) { }
                }
                foreach (var (d, (dong, tieuDe)) in c.Luoi)
                {
                    if (d.IsDisposed) continue;
                    if (d.RowTemplate.Height != dong) d.RowTemplate.Height = S(d.RowTemplate.Height);
                    if (d.ColumnHeadersHeight != tieuDe && d.ColumnHeadersHeightSizeMode != DataGridViewColumnHeadersHeightSizeMode.AutoSize)
                        d.ColumnHeadersHeight = S(d.ColumnHeadersHeight);
                }
            }
            catch (Exception ex) { Debug.WriteLine("[TyLeDpi] " + f.GetType().Name + ": " + ex.Message); }
            finally { f.ResumeLayout(true); }
        }
    }
}
