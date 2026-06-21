using PdfiumViewer;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PhanMemThiDua2026
{
    public partial class Form32_HuongDanPDF : Form
    {
        private PdfViewer pdfViewer;
        private static bool _pdfiumResolverSet = false;

        public Form32_HuongDanPDF()
        {
            InitializeComponent();
            // Chỉ nạp DLL 1 lần duy nhất cho vòng đời ứng dụng
            LoadPdfiumDll();
            InitViewer();
            this.FormClosed += (s, e) => DisposePdf();
        }

        private void InitViewer()
        {
            if (pdfViewer == null)
            {
                pdfViewer = new PdfViewer { Dock = DockStyle.Fill, ShowToolbar = true };
                Controls.Add(pdfViewer);
            }
        }

        public void GoiTenEmTrongDem_LoadPdf(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show("File PDF không tồn tại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                DisposePdf();
                pdfViewer.Document = PdfDocument.Load(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi load PDF: {ex.Message}");
                // Fallback: Mở bằng ứng dụng mặc định hệ thống
                TaTimThayEm_OpenPdfExternally(path);
                this.Close();
            }
        }

        private void TaTimThayEm_OpenPdfExternally(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở file: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void DisposePdf()
        {
            try
            {
                if (pdfViewer?.Document != null)
                {
                    pdfViewer.Document.Dispose();
                    pdfViewer.Document = null;
                }
            }
            catch { }
        }
        private void LoadPdfiumDll()
        {
            if (_pdfiumResolverSet) return;

            try
            {
                // 1. Tự động nhận diện CPU (64-bit hay 32-bit) để chọn đúng DLL
                string dllName = Environment.Is64BitProcess ? "pdfium-x64.dll" : "pdfium-x86.dll";
                string pdfPath = Path.Combine(AppContext.BaseDirectory, "Database", "HuongDanSuDung", dllName);

                if (!File.Exists(pdfPath))
                {
                    MessageBox.Show($"Lỗi bộ cài: Không tìm thấy thư viện tương thích kiến trúc máy tính '{dllName}'.\nVui lòng kiểm tra lại cấu hình hệ thống.", "Lỗi nghiêm trọng", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 2. Resolve (Hỗ trợ tốt nhất từ .NET Core 3.1 / .NET 6+)
                NativeLibrary.SetDllImportResolver(
                    typeof(PdfDocument).Assembly,
                    (name, assembly, path) =>
                        string.Equals(name, "pdfium.dll", StringComparison.OrdinalIgnoreCase)
                            ? NativeLibrary.Load(pdfPath)
                            : IntPtr.Zero
                );

                _pdfiumResolverSet = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nạp nền tảng hiển thị PDF: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}