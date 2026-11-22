namespace PhanMemThiDua2026
{
    /// <summary>
    /// Chạy một khối "trang trí giao diện" mà KHÔNG để các khung con tự dàn trang lại sau từng thay đổi.
    ///
    /// Vì sao: f.SuspendLayout() chỉ chặn bố cục của CHÍNH form. Mỗi lần gán Margin / Font / Anchor / RowStyles... cho một
    /// control nằm trong TableLayoutPanel, chính TableLayoutPanel đó vẫn dàn lại ngay (cột AutoSize phải đo lại mọi ô Krypton).
    /// Đo 22/11/2026 (bản Release, đã nóng JIT): Module_GiaoDienForm42_44Web.ApDung mất 180–250 ms/lần chỉ vì vậy;
    /// tạm dừng cả cây => ~5 ms, bố cục được dàn ĐÚNG 1 LẦN lúc mở lại.
    /// </summary>
    internal static class Module_TamDungBoCuc
    {
        public static void ChayKhongDanTrang(Control goc, Action viec)
        {
            var ds = new List<Control>(128);
            Gom(goc, ds);                                   // thứ tự trước-sau: cha đứng trước con
            foreach (var c in ds) c.SuspendLayout();
            try { viec(); }
            finally
            {
                // Mở lại TỪ TRÊN XUỐNG: cha dàn trang trước (đặt kích thước cho con, lúc đó con còn tạm dừng nên chỉ ghi nhận),
                // tới lượt con mới dàn 1 lần theo kích thước cuối cùng => không dàn lặp ở tầng dưới.
                // Control mới thêm trong lúc chạy (vd thẻ bo góc bọc ngoài GroupBox) không nằm trong ds: không bị tạm dừng, không sao.
                foreach (var c in ds)
                {
                    if (c.IsDisposed) continue;
                    try { c.ResumeLayout(true); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[TamDungBoCuc] " + ex.Message); }
                }
            }
        }
        private static void Gom(Control c, List<Control> ds)
        {
            ds.Add(c);
            foreach (Control con in c.Controls) Gom(con, ds);
        }
    }
}
