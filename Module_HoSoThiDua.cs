using System.Diagnostics;
using System.Runtime.InteropServices;
namespace PhanMemThiDua2026
{
    /// <summary>Form hồ sơ thi đua được giữ lại trong RAM để dùng lại (Form22 / Form62).</summary>
    internal interface IHoSoDungLai
    {
        /// <summary>true khi được Module_HoSoThiDua quản lý: bấm đóng chỉ ẩn, không hủy.</summary>
        bool DungLai { get; set; }
        /// <summary>true trong lúc dựng sẵn (hiện ngoài màn hình): không giành con trỏ khỏi cửa sổ đang dùng.</summary>
        bool DangDungSan { get; set; }
    }

    /// <summary>
    /// MỞ HỒ SƠ THI ĐUA TỨC THÌ (Form22 - CBCS, Form62 - Tân binh/Học viên) từ Form6 (xem) và Form15 (sửa).
    ///
    /// Đo 24/11/2026 (bench Release ngoài phần mềm): new Form22/Form30 chỉ ~10 ms, đọc + giải mã 6.000 dòng ~5 ms,
    /// nhưng Show() mất 430–520 ms MỖI LẦN — gần hết là tạo handle ~20 ComboBox (10–25 ms/ô, ô sau chậm hơn ô trước vì
    /// TableLayoutPanel dàn lại). Form trắng 20 ComboBox chuẩn của Windows cũng mất 100–200 ms. ShowDialog hủy handle khi
    /// đóng nên giữ lại đối tượng mà vẫn ShowDialog thì lần nào cũng chậm như nhau (~120 ms form trắng).
    ///
    /// Cách làm: mỗi loại giữ 1 form trong RAM; dựng sẵn lúc người dùng rảnh (tạo handle theo lát + chạy Load một lần ở ngoài
    /// màn hình, không kích hoạt); bấm đóng chỉ Hide. Mở = nạp dữ liệu (đồng bộ, vài ms) + Show (~10 ms).
    /// Form15 cần hộp thoại "modal": khóa cửa sổ cha bằng EnableWindow giống hệt ShowDialog (không đổi Enabled nên các control
    /// không bị vẽ lại màu xám), mở khóa trước khi ẩn để Windows trả con trỏ về đúng cửa sổ.
    /// </summary>
    internal static class Module_HoSoThiDua
    {
        private sealed class TrangThai
        {
            public IntPtr ChaBiKhoa;
            public Form? Cha;
            public Action? KhiLuu;
        }
        private static Form22_ChinhSuaDataCBCS? _f22;
        private static Form62_ChinhSuaDataNhomTuan? _f62;
        private static readonly Dictionary<Form, TrangThai> _trangThai = new();
        private static bool _dangDungSan;

        [DllImport("user32.dll")] private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
        private const int DWMWA_CLOAK = 13;

        private static Form22_ChinhSuaDataCBCS Lay22()
        {
            if (_f22 == null || _f22.IsDisposed) _f22 = Gan(new Form22_ChinhSuaDataCBCS { DungLai = true });
            return _f22;
        }
        private static Form62_ChinhSuaDataNhomTuan Lay62()
        {
            if (_f62 == null || _f62.IsDisposed) _f62 = Gan(new Form62_ChinhSuaDataNhomTuan { DungLai = true });
            return _f62;
        }
        private static T Gan<T>(T f) where T : Form
        {
            f.ShowInTaskbar = false;   // đặt TRƯỚC khi có handle (đổi sau sẽ dựng lại cửa sổ)
            f.Disposed += (_, _) => { MoKhoaCha(f); _trangThai.Remove(f); };
            return f;
        }

        /// <summary>Mở Form22 (CBCS). <paramref name="hopThoai"/> = khóa cửa sổ cha tới khi đóng (thay ShowDialog).</summary>
        public static void MoForm22(Control chu, bool hopThoai, int id, string soHieu, string hoTen, string donVi,
            string tinhTrang, bool chiXem, Action? khiLuu = null)
        {
            var f = Lay22();
            f.NapHoSo(id, soHieu, hoTen, donVi, tinhTrang, chiXem);
            Hien(f, chu, hopThoai, khiLuu);
        }
        /// <summary>Mở Form62 (Tân binh / Học viên - nhóm xét theo tuần, dựng theo Module_LichXet của chế độ hiện tại).</summary>
        public static void MoFormNhomTuan(Control chu, bool hopThoai, int id, string soHieu, string hoTen, string donVi,
            bool chiXem, Action? khiLuu = null)
        {
            var f = Lay62();
            f.NapHoSo(Module_CheDoDoiTuong.Hien, id, soHieu, hoTen, donVi, chiXem);
            Hien(f, chu, hopThoai, khiLuu);
        }

        private static void Hien(Form f, Control chu, bool hopThoai, Action? khiLuu)
        {
            if (!_trangThai.TryGetValue(f, out var tt)) _trangThai[f] = tt = new TrangThai();
            tt.KhiLuu = khiLuu;
            Form? cha = chu.TopLevelControl as Form ?? chu.FindForm();
            if (f.Visible)
            {
                // Đang mở (vd đang xem từ Form6): dữ liệu đã nạp lại, chỉ đưa lên trước
                if (hopThoai && tt.ChaBiKhoa == IntPtr.Zero) KhoaCha(f, tt, cha);
                if (f.WindowState == FormWindowState.Minimized) f.WindowState = FormWindowState.Normal;
                f.BringToFront();
                f.Activate();
                return;
            }
            CanGiua(f, cha);
            if (cha != null && !cha.IsDisposed && !ReferenceEquals(cha, f)) f.Show(cha);
            else f.Show();
            if (hopThoai) KhoaCha(f, tt, cha);
            f.Activate();
        }
        private static void CanGiua(Form f, Form? cha)
        {
            try
            {
                Rectangle vung = (cha != null && !cha.IsDisposed ? Screen.FromControl(cha) : Screen.PrimaryScreen)?.WorkingArea
                                 ?? Rectangle.Empty;
                if (vung.Width <= 0) return;
                f.StartPosition = FormStartPosition.Manual;
                f.Location = new Point(vung.Left + Math.Max(0, (vung.Width - f.Width) / 2),
                                       vung.Top + Math.Max(0, (vung.Height - f.Height) / 2));
            }
            catch (Exception ex) { Debug.WriteLine("[HoSoThiDua] CanGiua: " + ex.Message); }
        }
        private static void KhoaCha(Form f, TrangThai tt, Form? cha)
        {
            if (cha == null || cha.IsDisposed || !cha.IsHandleCreated || ReferenceEquals(cha, f)) return;
            tt.Cha = cha;
            tt.ChaBiKhoa = cha.Handle;
            EnableWindow(tt.ChaBiKhoa, false);
        }
        private static void MoKhoaCha(Form f)
        {
            if (!_trangThai.TryGetValue(f, out var tt) || tt.ChaBiKhoa == IntPtr.Zero) return;
            IntPtr h = tt.ChaBiKhoa;
            tt.ChaBiKhoa = IntPtr.Zero;
            try { if (IsWindow(h)) EnableWindow(h, true); } catch { }
        }

        /// <summary>
        /// Form gọi trong FormClosing (người dùng bấm đóng / nút Cập nhật). Trả true nếu đã xử lý = chỉ ẩn, form giữ lại.
        /// Thoát phần mềm / chủ sở hữu đóng => trả false để form đóng hẳn như thường.
        /// </summary>
        public static bool XuLyDong(Form f, FormClosingEventArgs e)
        {
            if (f is not IHoSoDungLai { DungLai: true }) return false;
            if (e.CloseReason != CloseReason.UserClosing || Module_ThoatAnToan.DangThoat)
            {
                MoKhoaCha(f);
                return false;
            }
            e.Cancel = true;
            _trangThai.TryGetValue(f, out var tt);
            // Mở khóa cha TRƯỚC khi ẩn (giống ShowDialog): Windows trả con trỏ về cửa sổ cha, không nhảy sang ứng dụng khác
            MoKhoaCha(f);
            bool daLuu = f.DialogResult == DialogResult.OK;
            f.Hide();
            try { if (tt?.Cha is { IsDisposed: false } c) c.Activate(); } catch { }
            var khiLuu = tt?.KhiLuu;
            if (tt != null) { tt.KhiLuu = null; tt.Cha = null; }
            if (daLuu && khiLuu != null)
            {
                try { khiLuu(); }
                catch (Exception ex) { Debug.WriteLine("[HoSoThiDua] KhiLuu: " + ex.Message); }
            }
            return true;
        }

        /// <summary>
        /// Dựng sẵn form hồ sơ của chế độ hiện tại lúc người dùng rảnh tay (Form6/Form15 gọi khi Load).
        /// Xếp hàng chung "lượt nặng" với các bộ dựng sẵn khác để không chạy dồn một nhịp.
        /// </summary>
        public static async Task DungSanAsync(Control chu)
        {
            if (_dangDungSan) return;
            _dangDungSan = true;
            try
            {
                bool Dung() => chu.IsDisposed || chu.Disposing || Module_ThoatAnToan.DangThoat;
                await Task.Delay(3000);
                if (!await Module_NhuongUI.ChoKhoChinhXongAsync(Dung)) return;
                using var luot = await Module_NhuongUI.GiuLuotNangAsync(Dung);
                if (luot == null) return;
                if (!await Module_NhuongUI.ChoRanhAsync(Dung, ranhMs: 500, toiDaMs: 15000)) return;
                // Form6 mở hồ sơ theo Số hiệu => phải giải mã cả cột SoHieu của bảng thi đua; lần đầu (bộ đệm giải mã lạnh)
                // ~200 ms/6.000 dòng, đã có bộ đệm ~10 ms. Làm nóng ở luồng nền (giải mã an toàn đa luồng).
                string bang = Module_CheDoDoiTuong.BangThiDuaThang;
                _ = Task.Run(() => LamNongGiaiMaSoHieu(bang));
                var cheDo = Module_CheDoDoiTuong.Hien;
                Form f;
                if (Module_CheDoDoiTuong.LaNhomTuan(cheDo)) { var f62 = Lay62(); f62.DamBaoBoCuc(cheDo); f = f62; }
                else f = Lay22();
                if (f.Visible || f.IsDisposed || ((IHoSoDungLai)f).DangDungSan) return;
                bool daCoHandle = f.IsHandleCreated;
                if (!daCoHandle && !await Module_NhuongUI.TaoHandleTheoLatAsync(f, () => Dung() || f.IsDisposed || f.Visible)) return;
                if (!await Module_NhuongUI.ChoRanhAsync(Dung, ranhMs: 300, toiDaMs: 5000)) return;
                if (f.Visible || f.IsDisposed || _daChayLoad.Contains(f)) return;
                ChayLoadNgoaiManHinh(f);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[HoSoThiDua] DungSan: " + ex.Message);
            }
            finally
            {
                _dangDungSan = false;
            }
        }
        private static void LamNongGiaiMaSoHieu(string bang)
        {
            try
            {
                using var cn = new Microsoft.Data.Sqlite.SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(Module_DanduongGPS.DuongDanCSDL4));
                cn.Open();
                using var cmd = cn.CreateCommand();
                cmd.CommandText = $"SELECT SoHieu FROM [{bang}]";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    if (Module_ThoatAnToan.DangThoat) return;
                    if (!r.IsDBNull(0)) _ = Module_BaoMatAES.GiaiMa(r.GetString(0));
                }
            }
            catch (Exception ex) { Debug.WriteLine("[HoSoThiDua] LamNong: " + ex.Message); }
        }
        private static readonly HashSet<Form> _daChayLoad = new();
        /// <summary>
        /// Hiện rồi ẩn ngay (đồng bộ) ở ngoài màn hình, bị cloak, không kích hoạt => Load + lần vẽ đầu chạy lúc rảnh.
        /// </summary>
        private static void ChayLoadNgoaiManHinh(Form f)
        {
            var d = (IHoSoDungLai)f;
            int bat = 1, tat = 0;
            d.DangDungSan = true;
            try
            {
                f.StartPosition = FormStartPosition.Manual;
                f.Location = new Point(-32000, -32000);
                try { DwmSetWindowAttribute(f.Handle, DWMWA_CLOAK, ref bat, sizeof(int)); } catch { }
                f.Show();
                f.Hide();
                _daChayLoad.Add(f);
                f.Disposed += (_, _) => _daChayLoad.Remove(f);
            }
            catch (Exception ex) { Debug.WriteLine("[HoSoThiDua] Load ngầm: " + ex.Message); }
            finally
            {
                d.DangDungSan = false;
                try { if (!f.IsDisposed && f.IsHandleCreated) DwmSetWindowAttribute(f.Handle, DWMWA_CLOAK, ref tat, sizeof(int)); } catch { }
            }
        }

        /// <summary>Đổi chế độ / khôi phục dữ liệu: bỏ các form đang giữ (lần mở sau dựng lại).</summary>
        public static void Huy()
        {
            foreach (Form? f in new Form?[] { _f22, _f62 })
            {
                try { if (f != null && !f.IsDisposed) { MoKhoaCha(f); f.Dispose(); } } catch { }
            }
            _f22 = null;
            _f62 = null;
        }
    }
}
