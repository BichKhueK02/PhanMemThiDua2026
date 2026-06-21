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
    }
}