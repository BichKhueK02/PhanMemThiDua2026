using System.Diagnostics;
namespace PhanMemThiDua2026
{
    internal static class Module_KhoiDongTrangChu
    {
        private static Form2_FormCha? _form2Instance;
        /// <summary>
        /// Preload Form2_FormCha vào RAM
        /// </summary>
        public static void PreloadForm2()
        {
            if (_form2Instance != null && !_form2Instance.IsDisposed)
                return;
            _form2Instance = new Form2_FormCha();
            _form2Instance.BeginInvoke(new Action(() =>
            {
                try
                {
                    // Tạo control để render UI nhẹ
                    _form2Instance.CreateControl();
                    _form2Instance.Refresh();
                    Debug.WriteLine("Form2 preload thành công.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi preload Form2: " + ex.Message);
                }
            }));
            // Preload form con Form15_ThongKeThiDua
            Task.Run(() =>
            {
                try
                {
                    _form2Instance.BeginInvoke(new Action(() =>
                    {
                        if (!_form2Instance.Controls.ContainsKey("Form15_ThongKeThiDua"))
                        {
                            _form2Instance.OpenChildForm<Form15_ThongKeThiDua>("");
                        }
                    }));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Preload Form15 lỗi: " + ex.Message);
                }
            });
        }
        /// <summary>
        /// Lấy instance Form2 đã preload
        /// </summary>
        public static Form2_FormCha? GetForm2()
        {
            if (_form2Instance == null || _form2Instance.IsDisposed)
                PreloadForm2();
            return _form2Instance;
        }
        public static void KiemTraVaCanhBaoTyLeManHinh()
        {
            try
            {
                float dpiX;
                // Lấy DPI thực tế của màn hình chính bằng Graphics Hwnd(0)
                using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
                {
                    dpiX = graphics.DpiX;
                }
                // Tính toán phần trăm Scale (100% = 96 DPI)
                float scalePercentage = (dpiX / 96f) * 100f;
                // Nếu tỷ lệ >= 150%
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