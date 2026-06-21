namespace PhanMemThiDua2026
{
    public static class Module_PhimTat
    {
        private static DateTime _lastKeyTime = DateTime.MinValue;
        private const int KEY_DELAY_MS = 200; // Tăng nhẹ lên 200ms để chống spam click nút an toàn hơn

        public static bool XuLy(
            Keys keyData,
            Action? actionLamMoi = null,
            Action? actionLuu_TinhToan = null,
            Action? actionXuatExcel = null,
            Action? actionTimKiem = null,
            Action? actionDongForm = null,
            Action? actionThemMoi = null,
            Action? actionSua = null,
            Action? actionXoa = null,
            Action? actionInAn = null,
            Action? actionTroGiup = null,
            Action? actionSelectAll = null,
            Action? actionHuyThaoTac = null,
            Action? actionXacNhan = null
            )
        {
            // ĐÃ XÓA CHẶN KEY Ở ĐÂY ĐỂ TRẢ LẠI TỐC ĐỘ GÕ CHỮ 100% CHO NGƯỜI DÙNG

            Keys key = keyData & Keys.KeyCode;
            Keys modifier = keyData & Keys.Modifiers;

            try
            {
                if (modifier == Keys.None)
                {
                    switch (key)
                    {
                        case Keys.F1: return Invoke(actionTroGiup);
                        case Keys.F2: return Invoke(actionSua);
                        case Keys.F5: return Invoke(actionLamMoi);
                        case Keys.Delete: return Invoke(actionXoa);
                        case Keys.Escape: return Invoke(actionDongForm);
                        case Keys.Enter: return Invoke(actionXacNhan);
                    }
                }

                if (modifier == Keys.Control)
                {
                    switch (key)
                    {
                        case Keys.A: return Invoke(actionSelectAll);
                        case Keys.Z: return Invoke(actionHuyThaoTac);
                        case Keys.N: return Invoke(actionThemMoi);
                        case Keys.S: return Invoke(actionLuu_TinhToan);
                        case Keys.D: return Invoke(actionXoa);
                        case Keys.E: return Invoke(actionXuatExcel);
                        case Keys.F: return Invoke(actionTimKiem);
                        case Keys.P: return Invoke(actionInAn);
                    }
                }

                if (modifier == Keys.Alt)
                {
                    switch (key)
                    {
                        case Keys.F4: return Invoke(actionDongForm);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi Module Phím Tắt [{keyData}]: {ex.Message}");
                return true;
            }

            // Nếu không trúng phím tắt nào -> lập tức trả về false để hệ điều hành lo việc gõ chữ
            return false;
        }

        // ======================================
        // HÀM GỌI (INVOKE) AN TOÀN VÀ CHỐNG SPAM
        // ======================================
        private static bool Invoke(Action? action)
        {
            if (action == null) return false;

            // CHỈ CHỐNG SPAM KHI ĐÓ LÀ PHÍM TẮT ĐƯỢC GỌI
            if ((DateTime.Now - _lastKeyTime).TotalMilliseconds < KEY_DELAY_MS)
            {
                return true; // Vẫn "nuốt" phím tắt này để chống gọi hàm 2 lần liên tục, nhưng ko ảnh hưởng gõ chữ
            }

            _lastKeyTime = DateTime.Now;
            action.Invoke();
            return true;
        }
    }
}