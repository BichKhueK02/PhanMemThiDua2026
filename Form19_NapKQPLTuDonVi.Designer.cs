namespace PhanMemThiDua2026
{
    // Giao diện của form này được dựng bằng code trong Form19_NapKQPLTuDonVi.cs (hàm BuildUi),
    // nên file Designer chỉ giữ lại phần quản lý tài nguyên.
    partial class Form19_NapKQPLTuDonVi
    {
        private System.ComponentModel.IContainer? components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                GiaiPhongTaiNguyenTuyChinh();
            }
            base.Dispose(disposing);
        }
    }
}