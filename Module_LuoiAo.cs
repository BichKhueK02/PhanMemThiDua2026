// Tiện ích dùng chung cho lưới ảo (VirtualMode) của Form6 / Form15 / Form34.
//
// Vấn đề: DataGridView.RowCount GIẢM từng dòng một -> mỗi lần bớt 1 dòng lưới lại tính lại bố cục, vùng hiển thị...
// Lọc 6000 người xuống ~3800 người = bớt ~2200 dòng = hàng chục giây (còn tăng dòng thì nhanh). Cách làm đúng:
// thay đổi lớn thì về 0 dòng (xóa 1 phát) rồi đặt lại đúng số dòng cần, và tắt vẽ trong lúc đó.
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    internal static class Module_LuoiAo
    {
        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        private static extern IntPtr GuiThongDiep(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private const int WM_SETREDRAW = 0x000B;
        private const int NGUONG_DOI_LON = 64;
        // Đếm lồng nhau theo từng lưới: chỉ bật vẽ lại khi lớp ngoài cùng kết thúc.
        private sealed class BoDem { public int Gia; }
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DataGridView, BoDem> _dem = new();
        public readonly struct PhamViTamDungVe : IDisposable
        {
            private readonly DataGridView? _g;
            internal PhamViTamDungVe(DataGridView? g) { _g = g; }
            public void Dispose()
            {
                var g = _g;
                if (g == null || g.IsDisposed) return;
                var d = _dem.GetOrCreateValue(g);
                if (d.Gia <= 0) return;
                if (--d.Gia != 0) return;
                if (!g.IsHandleCreated) return;
                GuiThongDiep(g.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
                g.Invalidate();
            }
        }
        /// <summary>Tắt vẽ lưới cho tới khi Dispose (dùng với <c>using var _ = Module_LuoiAo.TamDungVe(grid);</c>).</summary>
        public static PhamViTamDungVe TamDungVe(DataGridView? g)
        {
            if (g == null || g.IsDisposed) return new PhamViTamDungVe(null);
            var d = _dem.GetOrCreateValue(g);
            if (d.Gia++ == 0 && g.IsHandleCreated)
                GuiThongDiep(g.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            return new PhamViTamDungVe(g);
        }
        /// <summary>
        /// Đặt số dòng cho lưới ảo nhanh. boChon = true: bỏ chọn (dùng khi danh sách hiển thị đã đổi sang tập người khác,
        /// tránh việc dòng đang chọn trỏ nhầm sang người khác).
        /// </summary>
        public static void DatSoDong(DataGridView? g, int soDong, bool boChon)
        {
            if (g == null || g.IsDisposed || g.ColumnCount == 0) return;
            if (soDong < 0) soDong = 0;
            if (boChon)
            {
                try { g.CurrentCell = null; g.ClearSelection(); } catch { /* lưới đang ở trạng thái không cho đổi ô */ }
            }
            int cu = g.RowCount;
            if (cu == soDong) return;
            // Tự đo chiều cao theo nội dung ở lưới ảo = đo cả nghìn dòng -> tắt trong lúc đổi số dòng
            var cheDoHang = g.AutoSizeRowsMode;
            bool tat = cheDoHang != DataGridViewAutoSizeRowsMode.None;
            try
            {
                if (tat) g.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
                if (soDong < cu && cu - soDong > NGUONG_DOI_LON)
                {
                    try { g.CurrentCell = null; } catch { }
                    g.RowCount = 0;           // xóa 1 phát thay vì bớt từng dòng
                    if (soDong > 0) g.RowCount = soDong;
                }
                else
                {
                    g.RowCount = soDong;      // đổi ít dòng: đặt thẳng (giữ vị trí cuộn / dòng đang chọn)
                }
            }
            finally
            {
                if (tat && cheDoHang != DataGridViewAutoSizeRowsMode.AllCells && cheDoHang != DataGridViewAutoSizeRowsMode.AllHeaders)
                    g.AutoSizeRowsMode = cheDoHang;
            }
        }
    }
}