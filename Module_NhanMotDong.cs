using System.ComponentModel;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Nhãn LUÔN 1 DÒNG: chữ dài thì cắt cuối bằng "…" thay vì rớt dòng (Label thường tự xuống dòng khi AutoSize = false
    /// và ô đủ cao => đầu trang xấu). Kế thừa Label nên dùng được trong trình thiết kế Visual Studio như Label thường.
    /// Căn chữ theo TextAlign (trái / giữa / phải, trên / giữa / dưới).
    /// </summary>
    [ToolboxItem(true)]
    public class NhanMotDong : Label
    {
        public NhanMotDong()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            AutoSize = false;
        }
        /// <summary>Cờ vẽ chữ dùng chung cho vẽ và đo (đo khớp đúng cách vẽ).</summary>
        public TextFormatFlags CoVe
        {
            get
            {
                var c = TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
                c |= TextAlign switch
                {
                    ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight => TextFormatFlags.Top,
                    ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight => TextFormatFlags.Bottom,
                    _ => TextFormatFlags.VerticalCenter
                };
                c |= TextAlign switch
                {
                    ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter => TextFormatFlags.HorizontalCenter,
                    ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight => TextFormatFlags.Right,
                    _ => TextFormatFlags.Left
                };
                if (RightToLeft == RightToLeft.Yes) c |= TextFormatFlags.RightToLeft;
                return c;
            }
        }
        /// <summary>Vùng vẽ chữ (trừ Padding).</summary>
        public Rectangle VungChu => new(Padding.Left, Padding.Top,
            Math.Max(0, ClientSize.Width - Padding.Horizontal), Math.Max(0, ClientSize.Height - Padding.Vertical));
        /// <summary>Chữ <paramref name="chu"/> với font <paramref name="f"/> có nằm gọn 1 dòng không (không phải cắt "…").</summary>
        public bool VuaMotDong(string chu, Font f) =>
            TextRenderer.MeasureText(chu, f, new Size(int.MaxValue, int.MaxValue), CoVe & ~TextFormatFlags.EndEllipsis).Width <= VungChu.Width;
        protected override void OnPaint(PaintEventArgs e)
        {
            // Không gọi base.OnPaint: Label gốc vẽ chữ có xuống dòng
            Color mau = Enabled ? ForeColor : SystemColors.GrayText;
            TextRenderer.DrawText(e.Graphics, Text, Font, VungChu, mau, CoVe);
        }
    }
}
