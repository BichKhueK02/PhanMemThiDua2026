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
            if (_pdfiumResolverSet)
                return;

            try
            {
                // Phần mềm chỉ phát hành x64
                const string dllName = "pdfium-x64.dll";

                string pdfPath = Path.Combine(
                    Module_DanduongGPS.ThuMucHuongDan,
                    dllName);

                // Kiểm tra thư viện trước khi đăng ký Resolver
                if (!File.Exists(pdfPath))
                {
                    MessageBox.Show(
                        $"Lỗi bộ cài: Không tìm thấy thư viện PDFium:\n\n{pdfPath}\n\n" +
                        "Vui lòng kiểm tra lại thư mục HuongDanSuDung.",
                        "Lỗi nghiêm trọng",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                NativeLibrary.SetDllImportResolver(
                    typeof(PdfDocument).Assembly,
                    (name, assembly, path) =>
                    {
                        if (!string.Equals(
                                name,
                                "pdfium.dll",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            return IntPtr.Zero;
                        }

                        return NativeLibrary.Load(pdfPath);
                    });

                _pdfiumResolverSet = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi nạp thư viện PDFium:\n\n{ex.Message}",
                    "Lỗi hệ thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}