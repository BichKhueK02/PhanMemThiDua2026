using System.Diagnostics;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Bản rút gọn: chỉ giữ việc cảnh báo tỷ lệ màn hình (Program gọi).
    /// Phần PreloadForm2/GetForm2 cũ đã bỏ vì không còn dùng (việc nạp trước Form2 do Module_GiaoDienForm2Web đảm nhiệm)
    /// và còn gọi BeginInvoke khi form chưa có handle nên sẽ lỗi nếu bị gọi.
    /// </summary>
    internal static class Module_KhoiDongTrangChu
    {
        public static void KiemTraVaCanhBaoTyLeManHinh()
        {
            try
            {
                float dpiX;
                // Lấy DPI thực tế của màn hình chính
                using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
                {
                    dpiX = graphics.DpiX;
                }
                float scalePercentage = (dpiX / 96f) * 100f; // 100% = 96 DPI
                if (scalePercentage >= 150f)
                {
                    MessageBox.Show(
                        $"Hệ thống phát hiện tỷ lệ hiển thị màn hình (Scale/Zoom) của máy tính đang ở mức {Math.Round(scalePercentage)}%.\n\n" +
                        "Điều này có thể làm một số giao diện của phần mềm bị phóng to quá mức, che khuất nút bấm hoặc hiển thị không chính xác.\n\n" +
                        "Khuyến nghị: Nhấn chuột phải vào màn hình Desktop -> Chọn 'Display settings' -> Chỉnh mục 'Scale' về mức 100% hoặc tối đa 125% để có trải nghiệm tốt nhất.",
                        "Cảnh báo tỷ lệ màn hình",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi kiểm tra DPI màn hình: " + ex.Message);
            }
        }
    }
}