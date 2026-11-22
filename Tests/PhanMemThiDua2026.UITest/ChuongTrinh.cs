using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026.UITest
{
    /// <summary>
    /// Hai chế độ của cùng một exe:
    ///  • Không tham số  => ĐIỀU PHỐI: hỏi số lần chạy (1-400), lần lượt mở tiến trình con cho từng lần, chờ xong,
    ///                      rồi ghi báo cáo tổng hợp uitest-&lt;đợt&gt;-TONGHOP.log và mở bằng Notepad.
    ///  • --lan k n đợt  => MỘT LẦN CHẠY: chạy NGUYÊN luồng khởi động thật của phần mềm (Program.Main qua reflection:
    ///                      khóa, CSDL, theme, Form1...); luồng nền đợi Form1 hiện ra rồi chuyển bộ bấm thử vào luồng giao diện.
    /// Mỗi lần là một tiến trình riêng vì phần mềm dùng khóa một phiên, trạng thái tĩnh và Environment.Exit khi thoát.
    /// </summary>
    internal static class ChuongTrinh
    {
        public static volatile bool DaXong;
        private delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint threadId, EnumProc cb, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [STAThread]
        private static int Main(string[] args)
        {
            // Tiến trình con: --lan k n đợt [--cauhinh "đường dẫn uitest.json"]  (chạy từ thư mục phần mềm được test)
            if (args.Length >= 4 && args[0] == "--lan"
                && int.TryParse(args[1], out int lan) && int.TryParse(args[2], out int tong))
            {
                int i = Array.IndexOf(args, "--cauhinh");
                if (i >= 0 && i + 1 < args.Length) CauHinh.DuongDan = args[i + 1];
                return ChayMotLan(lan, tong, args[3]);
            }
            return DieuPhoi.Chay();
        }
        // =============================================================== MỘT LẦN CHẠY (tiến trình con)
        private static int ChayMotLan(int lan, int tong, string dot)
        {
            var ch = CauHinh.Doc(out string loi);
            if (ch == null)
            {
                MessageBox.Show(loi, "PhanMemThiDua2026.UITest", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 1;
            }
            BaoCao.DatTen(dot, lan);
            BaoCao.KhoiDong();
            BaoCao.Ghi($"Lần chạy: {lan}/{tong}");
            BaoCao.Ghi("");
            // Phần mềm có chỗ gọi Environment.Exit (đóng Form2, thoát dứt khoát...) => Main bên dưới không chạy tiếp được;
            // ProcessExit vẫn chạy => vẫn ghi được bước cuối + tổng kết.
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                if (DaXong) return;
                DaXong = true;
                BaoCao.SuKien(KetQua.Loi, $"Phần mềm TỰ TẮT trước khi kiểm thử xong. Bước cuối: {BaoCao.TenBuocHienTai}");
                BaoCao.TongKet();
            };
            uint luongGiaoDien = HopThoaiHeThong.GetCurrentThreadId();
            HopThoaiHeThong.BatDau(luongGiaoDien, ch);
            // Không tạo cửa sổ nào ở đây: Program.Main còn phải đặt DPI/visual styles trước cửa sổ đầu tiên.
            new Thread(() => ChoForm1RoiBatDau(luongGiaoDien, ch)) { IsBackground = true, Name = "UITest - chờ Form1" }.Start();
            System.Reflection.Assembly asm;
            try { asm = NapPhanMem.Nap(AppContext.BaseDirectory); }
            catch (Exception ex)
            {
                BaoCao.SuKien(KetQua.Loi, "Không nạp được PhanMemThiDua2026.dll: " + ex.Message);
                DaXong = true;
                BaoCao.TongKet();
                return 1;
            }
            BaoCao.Ghi($"Phiên bản được test: {NapPhanMem.PhienBan(AppContext.BaseDirectory)}");
            var main = asm.GetType("PhanMemThiDua2026.Program")?.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
            if (main == null)
            {
                BaoCao.SuKien(KetQua.Loi, "Không tìm thấy PhanMemThiDua2026.Program.Main");
                DaXong = true;
                BaoCao.TongKet();
                return 1;
            }
            int ma;
            try { ma = main.Invoke(null, null) is int i ? i : 0; }
            catch (TargetInvocationException ex)
            {
                BaoCao.SuKien(KetQua.Loi, "Phần mềm sập: " + ex.InnerException);
                ma = 1;
            }
            if (!DaXong)
            {
                DaXong = true;
                BaoCao.SuKien(KetQua.Loi, $"Phần mềm tự thoát (mã {ma}) trước khi kiểm thử xong. Bước cuối: {BaoCao.TenBuocHienTai}");
                BaoCao.TongKet();
            }
            return ma;
        }
        private static void ChoForm1RoiBatDau(uint luong, CauHinh ch)
        {
            var han = DateTime.Now.AddMinutes(5);
            while (DateTime.Now < han)
            {
                Thread.Sleep(300);
                Form? form1 = null;
                EnumThreadWindows(luong, (h, _) =>
                {
                    if (!IsWindowVisible(h)) return true;
                    if (Control.FromHandle(h) is Form f && f.GetType().Name == "Form1") { form1 = f; return false; }
                    return true;
                }, IntPtr.Zero);
                if (form1 == null) continue;
                try
                {
                    form1.BeginInvoke(new Action(() => TuDongBam.BatDau(ch, form1)));
                    return;
                }
                catch { /* handle chưa sẵn sàng: thử lại */ }
            }
            BaoCao.SuKien(KetQua.Loi, "Sau 5 phút không thấy màn hình đăng nhập (Form1)");
        }
        /// <summary>Mở tệp bằng Notepad để xem ngay.</summary>
        public static void MoTep(string tep)
        {
            try { Process.Start(new ProcessStartInfo("notepad.exe", "\"" + tep + "\"") { UseShellExecute = true }); }
            catch { }
        }
    }
}
