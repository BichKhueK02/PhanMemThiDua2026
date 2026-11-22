using System.Reflection;
using System.Runtime.Loader;
namespace PhanMemThiDua2026.UITest
{
    /// <summary>
    /// Nạp PhanMemThiDua2026.dll của BẢN ĐƯỢC CHỌN lúc chạy (không tham chiếu cứng lúc biên dịch) => test được mọi bản build
    /// (Debug, Release, bản cũ...) miễn thư mục đó có PhanMemThiDua2026.dll + PhanMemThiDua2026.deps.json.
    /// Thư viện phụ thuộc (Krypton, Sqlite, ClosedXML...) và thư viện native (e_sqlite3...) được tìm theo deps.json
    /// của chính phần mềm (AssemblyDependencyResolver).
    /// Bản publish 1 tệp (PublishSingleFile) KHÔNG có dll riêng => không nạp được.
    /// </summary>
    internal static class NapPhanMem
    {
        public const string TEN_DLL = "PhanMemThiDua2026.dll";
        public const string TEN_EXE = "PhanMemThiDua2026.exe";
        private static Assembly? _asm;
        public static Assembly Asm => _asm ?? throw new InvalidOperationException("Chưa nạp phần mềm");
        /// <summary>Kiểm tra thư mục có test được không. Trả về null nếu được, ngược lại là lý do.</summary>
        public static string? KiemTraThuMuc(string thuMuc)
        {
            if (string.IsNullOrWhiteSpace(thuMuc) || !Directory.Exists(thuMuc)) return "Thư mục không tồn tại.";
            string dll = Path.Combine(thuMuc, TEN_DLL);
            if (!File.Exists(dll))
            {
                if (File.Exists(Path.Combine(thuMuc, TEN_EXE)))
                    return "Đây là bản publish 1 tệp (single-file): không có PhanMemThiDua2026.dll nên không test được.\n" +
                           "Hãy chọn thư mục BUILD, ví dụ bin\\Release\\net8.0-windows hoặc bin\\Debug\\net8.0-windows.";
                return "Không thấy PhanMemThiDua2026.dll trong thư mục này.";
            }
            if (!File.Exists(Path.Combine(thuMuc, "PhanMemThiDua2026.deps.json")))
                return "Thiếu PhanMemThiDua2026.deps.json (thư mục build không đầy đủ).";
            return null;
        }
        public static string PhienBan(string thuMuc)
        {
            try
            {
                var v = System.Diagnostics.FileVersionInfo.GetVersionInfo(Path.Combine(thuMuc, TEN_DLL));
                return $"{v.FileVersion} (build {File.GetLastWriteTime(Path.Combine(thuMuc, TEN_DLL)):dd/MM/yyyy HH:mm})";
            }
            catch { return "?"; }
        }
        /// <summary>Nạp phần mềm trong thư mục của tiến trình hiện tại (tiến trình con luôn chạy từ thư mục phần mềm).</summary>
        public static Assembly Nap(string thuMuc)
        {
            string dll = Path.Combine(thuMuc, TEN_DLL);
            var resolver = new AssemblyDependencyResolver(dll);
            AssemblyLoadContext.Default.Resolving += (ctx, ten) =>
            {
                string? p = resolver.ResolveAssemblyToPath(ten);
                if (p == null)
                {
                    string thu = Path.Combine(thuMuc, ten.Name + ".dll");
                    if (File.Exists(thu)) p = thu;
                }
                return p != null ? ctx.LoadFromAssemblyPath(p) : null;
            };
            AssemblyLoadContext.Default.ResolvingUnmanagedDll += (asm, ten) =>
            {
                string? p = resolver.ResolveUnmanagedDllToPath(ten);
                return p != null && System.Runtime.InteropServices.NativeLibrary.TryLoad(p, out var h) ? h : IntPtr.Zero;
            };
            _asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
            return _asm;
        }
    }
}
