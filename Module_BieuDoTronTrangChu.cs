using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhanMemThiDua2026
{
    internal static class Module_BieuDoTronTrangChu
    {
        /// <summary>
        /// Cập nhật lại biểu đồ tròn trên Form4 (Trang chủ).
        /// Có thể gọi từ bất kỳ Form nào sau khi dữ liệu thay đổi.
        /// </summary>
        public static async Task CapNhatBieuDoForm4Async()
        {
            var frm4 = Application.OpenForms
                                  .OfType<Form4_TrangDauTien>()
                                  .FirstOrDefault();

            if (frm4 == null || frm4.IsDisposed)
                return;

            await frm4.RefreshPieChartAsync();
        }
    }
}