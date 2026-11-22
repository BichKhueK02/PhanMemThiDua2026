//namespace PhanMemThiDua2026
namespace PhanMemThiDua2026
{
    // Giao diện của form này được dựng bằng code trong Form28_DataTrungTen.cs (hàm BuildUi),
    // nên file Designer chỉ giữ lại phần quản lý tài nguyên.
    partial class Form28_DataTrungTen
    {
        private System.ComponentModel.IContainer components = null;
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
