using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// JIT SONG SONG: biên dịch trước (JIT) các phương thức của những lớp sắp dùng ở MỘT LUỒNG NỀN ưu tiên thấp,
    /// trong lúc luồng giao diện còn đang hiện form đăng nhập. Lúc dựng Form2/Form4/Form6/... luồng giao diện không còn
    /// phải tự JIT các InitializeComponent khổng lồ (hàng trăm control) nữa.
    ///
    /// - Chỉ BIÊN DỊCH, không chạy phương thức nào, không tạo control nào (an toàn với luồng nền).
    /// - Bỏ qua phương thức generic mở, abstract, P/Invoke, extern. Mọi lỗi đều bị nuốt.
    /// - Duyệt cả lớp lồng nhau (lớp đóng gói của lambda, state machine của async).
    /// - Bật bằng 1 dòng trong constructor Form1: Module_JitSong.BatDauNen(typeof(...), ...). Muốn tắt: xóa dòng đó.
    /// </summary>
    internal static class Module_JitSong
    {
        private const BindingFlags TatCa =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private static int _daChay;
        public static void BatDauNen(params Type[] cacLoai)
        {
            if (cacLoai.Length == 0 || Environment.ProcessorCount < 2) return;   // 1 nhân: JIT nền chỉ tranh CPU với giao diện
            if (Interlocked.Exchange(ref _daChay, 1) != 0) return;
            try
            {
                new Thread(() => Chay(cacLoai))
                {
                    IsBackground = true,
                    Name = "JitSong",
                    Priority = ThreadPriority.BelowNormal
                }.Start();
            }
            catch { /* không chạy được thì thôi, chương trình vẫn chạy bình thường */ }
        }
        private static void Chay(Type[] cacLoai)
        {
            var sw = Stopwatch.StartNew();
            int tong = 0;
            foreach (var loai in cacLoai)
            {
                try { tong += JitLoai(loai, 0); }
                catch (Exception ex) { Debug.WriteLine($"[JitSong] {loai?.Name}: {ex.Message}"); }
            }
            Debug.WriteLine($"[JitSong] đã JIT {tong} phương thức của {cacLoai.Length} lớp trong {sw.ElapsedMilliseconds} ms (luồng nền)");
        }
        private static int JitLoai(Type loai, int sau)
        {
            if (loai.ContainsGenericParameters || sau > 3) return 0;
            int dem = 0;
            foreach (var c in loai.GetConstructors(TatCa)) dem += Thu(c);
            foreach (var m in loai.GetMethods(TatCa)) dem += Thu(m);
            foreach (var con in loai.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)) dem += JitLoai(con, sau + 1);
            return dem;
        }
private static int Thu(MethodBase m)
    {
        try
        {
            if (m == null || m.IsAbstract || m.ContainsGenericParameters)
                return 0;
            if ((m.Attributes & MethodAttributes.PinvokeImpl) != 0)
                return 0;
            if ((m.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) != 0)
                return 0;
            RuntimeHelpers.PrepareMethod(m.MethodHandle);
            return 1;
        }
        catch
        {
            return 0;
        }
    }
}
}