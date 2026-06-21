namespace PhanMemThiDua2026
{
    public static class Module_ThoatAnToan
    {
        private static bool isExiting = false; // Tránh gọi nhiều lần
        /// <summary>
        /// Kích hoạt ESC cho Form
        /// </summary>
        public static void KichHoatESC(Form frm)
        {
            if (frm == null) return;

            frm.KeyPreview = true; // Form nhận key trước các control

            // Chỉ thêm handler nếu chưa được thêm
            if (!isHandlerRegistered(frm))
            {
                frm.KeyDown += FrmKeyDown;
                MarkHandlerRegistered(frm);
            }
        }
        private static void FrmKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && !isExiting)
            {
                isExiting = true;

                try
                {
                    DialogResult result = MessageBox.Show(
                        "Bạn có muốn thoát phần mềm ngay bây giờ?",
                        "Thoát khẩn cấp",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );

                    if (result == DialogResult.Yes)
                    {
                        ThoatAnToan();
                    }
                }
                catch
                {
                    ThoatAnToan();
                }
                finally
                {
                    isExiting = false;
                }
            }
        }
        /// <summary>
        /// Thoát phần mềm an toàn
        /// </summary>
        public static void ThoatAnToan()
        {
            try
            {
                foreach (Form f in Application.OpenForms)
                {
                    if (!f.IsDisposed)
                        f.Close();
                }
            }
            catch { }
            finally
            {
                Application.Exit();
            }
        }
        #region Private helper để tránh thêm handler nhiều lần
        private static readonly HashSet<Form> registeredForms = new();

        private static bool isHandlerRegistered(Form frm) => registeredForms.Contains(frm);
        private static void MarkHandlerRegistered(Form frm) => registeredForms.Add(frm);
        #endregion
    }
}
