using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
using System.Text;

namespace PhanMemThiDua2026
{
    public partial class Form34_ThongKeKhenThuong : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;
        // Biến phục vụ chức năng lọc mượt mà (Debounce)
        private System.Windows.Forms.Timer timKiemTimer;
        public bool DaLoadDuLieu { get; private set; } = false; // Đường dẫn CSDL
        // Biến lưu trữ dữ liệu gốc để lọc trên RAM  
        private bool isUpdatingCombo = false;
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lParam);
        private const int WM_SETREDRAW = 11;
        // 🔥 THÊM BIẾN CACHE HÌNH ẢNH Ở ĐÂY
        private readonly Image _iconKhenThuong = Properties.Resources.ic_khenthuong;
        // Đặt class này trong namespace hoặc ngay trong class Form34_ThongKeKhenThuong
        public class ThongTinKhenThuongCBCS
        {
            public long ID { get; set; }
            public string HoVaTen { get; set; }
            public string SoHieu { get; set; }
            public string DonVi { get; set; }
            public string TinhTrang { get; set; }
            public int SoLuong_Khen { get; set; }
            public string GhiChu_Khen { get; set; }
            public string GhiChu_DanhSach { get; set; }
            public string DanhSachDVKhen_An { get; set; }
        }
        // XÓA BIẾN CŨ: private DataTable dtDanhSachHienThi;
        // THÊM 2 BIẾN MỚI THAY THẾ:
        private List<ThongTinKhenThuongCBCS> _danhSachGoc = new List<ThongTinKhenThuongCBCS>();
        private List<ThongTinKhenThuongCBCS> _danhSachHienThi = new List<ThongTinKhenThuongCBCS>();
        public Form34_ThongKeKhenThuong()
        {
            InitializeComponent();

            this.DoubleBuffered = true;

            if (textBox_TimKiemTheoTen != null)
            {
                textBox_TimKiemTheoTen.TextChanged += TextBox_TimKiemTheoTen_TextChanged;
            }

            if (comboBox_TimKiemDonVi != null)
                comboBox_TimKiemDonVi.SelectedIndexChanged += BoLoc_ValueChanged;

            if (comboBox_TinhTrangCongTac != null)
                comboBox_TinhTrangCongTac.SelectedIndexChanged += BoLoc_ValueChanged;

            if (comboBox1_DonViKhenThuong != null)
                comboBox1_DonViKhenThuong.SelectedIndexChanged += BoLoc_ValueChanged;

            if (kryptonDataGridView1_DanhSachCBCS != null)
            {
                kryptonDataGridView1_DanhSachCBCS.CellClick += KryptonDataGridView1_DanhSachCBCS_CellClick;
                kryptonDataGridView1_DanhSachCBCS.RowPostPaint += KryptonDataGridView1_DanhSachCBCS_RowPostPaint;
                kryptonDataGridView1_DanhSachCBCS.CellDoubleClick += KryptonDataGridView1_DanhSachCBCS_CellDoubleClick;

                // ⭐ THÊM MỚI 1: Gán ContextMenuStrip vào DataGridView
                kryptonDataGridView1_DanhSachCBCS.ContextMenuStrip = contextMenuStrip1;

                // ⭐ THÊM MỚI 2: Đăng ký sự kiện MouseDown để tự động chọn dòng khi Click chuột phải
                kryptonDataGridView1_DanhSachCBCS.MouseDown += KryptonDataGridView1_DanhSachCBCS_MouseDown;

                // 🔥 THÊM SỰ KIỆN VẼ CELL Ở ĐÂY ĐỂ HIỂN THỊ ICON
                kryptonDataGridView1_DanhSachCBCS.CellPainting -= KryptonDataGridView1_DanhSachCBCS_CellPainting;
                kryptonDataGridView1_DanhSachCBCS.CellPainting += KryptonDataGridView1_DanhSachCBCS_CellPainting;
            }

            timKiemTimer = new System.Windows.Forms.Timer();
            timKiemTimer.Interval = 300;
            timKiemTimer.Tick += TimKiemTimer_Tick;
            // ⭐ GỌI HÀM HIỆU ỨNG TẠI ĐÂY (Ở CUỐI CÙNG LÀ TỐT NHẤT)
            ĐangKyHieuUngVienTextBox();
        }
        private void TextBox_TimKiemTheoTen_TextChanged(object sender, EventArgs e)
        {
            // Reset lại timer mỗi khi người dùng gõ phím
            if (timKiemTimer != null)
            {
                timKiemTimer.Stop();
                timKiemTimer.Start();
            }
        }
        private void TimKiemTimer_Tick(object sender, EventArgs e)
        {
            // Người dùng đã ngừng gõ 300ms -> Bắt đầu lọc
            timKiemTimer.Stop();
            BoLoc_ValueChanged(sender, e);
        }
        private async void Form34_ThongKeKhenThuong_Load(object sender, EventArgs e)
        {
            CauHinhStatusStrip();
            LoadComboBoxTinhTrang();
            KhoaCacTextBox();
            // Gọi hàm load dữ liệu ở đây
            await ReloadDuLieu();
        }
        // Thiết lập chế độ Chỉ Đọc (Không cho phép sửa chữa)
        private void KhoaCacTextBox()
        {
            if (kryptonTextBox1_STT != null) kryptonTextBox1_STT.ReadOnly = true;
            if (kryptonTextBox1_HoVaTen != null) kryptonTextBox1_HoVaTen.ReadOnly = true;
            if (kryptonTextBox1_SoHieu != null) kryptonTextBox1_SoHieu.ReadOnly = true;
            if (kryptonTextBox1_DonVi != null) kryptonTextBox1_DonVi.ReadOnly = true;
            if (kryptonTextBox1_TinhTrang != null) kryptonTextBox1_TinhTrang.ReadOnly = true;
            if (kryptonTextBox1_SoLuong != null) kryptonTextBox1_SoLuong.ReadOnly = true;
        }
        // =========================================================================
        // HÀM BẢO VỆ DELEGATE (CHỐNG CRASH)
        // =========================================================================
        private Action SafeAction(Action action)
        {
            return () =>
            {
                if (action == null) return;
                try { action.Invoke(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine("Lỗi thực thi phím tắt: " + ex.Message); }
            };
        }
        private bool SafeExecute(Action action)
        {
            SafeAction(action).Invoke();
            return true;
        }
        // =========================================================================
        // BẮT PHÍM TẮT TRÊN FORM
        // =========================================================================
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            try
            {
                // 1. GỌI MODULE PHÍM TẮT CHUNG 
                if (Module_PhimTat.XuLy(
                    keyData,
                    actionLamMoi: SafeAction(() => lamMoi_ToolStripMenuItem.PerformClick()), // F5
                    actionLuu_TinhToan: SafeAction(() => capNhatThongTinKhenThuong_ToolStripMenuItem.PerformClick()) // Ctrl + S
                ))
                {
                    return true;
                }

                // 2. PHÍM TẮT ĐẶC THÙ RIÊNG CỦA FORM NÀY
                Keys key = keyData & Keys.KeyCode;
                Keys modifier = keyData & Keys.Modifiers;

                if (modifier == Keys.None)
                {
                    switch (key)
                    {
                        case Keys.F4: // F4: Xóa tìm kiếm
                            return SafeExecute(() => xoaTimKiem_ToolStripMenuItem.PerformClick());
                    }
                }
                else if (modifier == Keys.Control)
                {
                    switch (key)
                    {
                        case Keys.P: // Ctrl + P: Phân tích khen thưởng
                            return SafeExecute(() => phanTichKhenThuong_ToolStripMenuItem.PerformClick());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi phím tắt: " + ex.Message);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
        // Thay thế toàn bộ hàm ReloadDuLieu trong Form 36
        // Tạo một class tạm để hứng dữ liệu chưa giải mã (Raw Data)
        // Cấu trúc tạm để lưu dữ liệu mã hóa từ SQLite
        // =========================================================
        // HÀM HỖ TRỢ: GIẢI MÃ AN TOÀN (CHỐNG CRASH)
        // =========================================================
        private string SafeDecrypt(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            input = input.Trim();

            string decoded = BaoMatAES.GiaiMa(input);

            // Nếu giải mã thành công
            if (!string.IsNullOrEmpty(decoded)) return decoded;

            // Nếu thất bại (trả về rỗng) mà có mác v2| thì báo lỗi
            if (input.StartsWith("v2|")) return "[LỖI GIẢI MÃ]";

            // Nếu không có mác v2|, có thể là dữ liệu thô cũ
            return input;
        }
        //public async Task ReloadDuLieu()
        //{
        //    if (isUpdatingCombo) return;

        //    try
        //    {
        //        // 1. HIỆN GIAO DIỆN CHỜ
        //        if (toolStripProgressBar1_LamMoi != null)
        //        {
        //            toolStripProgressBar1_LamMoi.Visible = true;
        //            toolStripProgressBar1_LamMoi.Style = ProgressBarStyle.Marquee;
        //        }

        //        // 2. KHÓA VẼ GIAO DIỆN TỪ CẤP ĐỘ HỆ ĐIỀU HÀNH (CHỐNG TREO, CHỐNG NHÁY)
        //        SendMessage(kryptonDataGridView1_DanhSachCBCS.Handle, WM_SETREDRAW, 0, null);
        //        kryptonDataGridView1_DanhSachCBCS.SuspendLayout();

        //        // 🔥 VIRTUAL MODE: Bỏ dùng DataSource hoàn toàn
        //        // kryptonDataGridView1_DanhSachCBCS.DataSource = null; 

        //        // 3. XỬ LÝ DỮ LIỆU ĐA LUỒNG DƯỚI BACKGROUND
        //        var listNew = await Task.Run(() =>
        //        {
        //            bool laTanBinh = Module_TaiKhoan.LayPhienBanPhanMem().Contains("tân binh", StringComparison.OrdinalIgnoreCase);
        //            string tableThiDua = laTanBinh ? "ThiDuaThang_TanBinh" : "ThiDuaThang";

        //            // ======================================================================
        //            // GIỮ NGUYÊN 100% CODE GỐC LẤY 3 DICTIONARY CỦA BẠN TỪ SQLITE
        //            // ======================================================================
        //            var dictDanhSach = new Dictionary<string, string>();
        //            using (var cn2 = new SqliteConnection($"Data Source={_csdl2Path}"))
        //            {
        //                cn2.Open();
        //                using var cmd = new SqliteCommand("SELECT SoHieu, GhiChu FROM DanhSach", cn2);
        //                using var rd = cmd.ExecuteReader();
        //                while (rd.Read())
        //                {
        //                    string sh = SafeDecrypt(rd["SoHieu"]?.ToString());
        //                    if (!string.IsNullOrEmpty(sh)) dictDanhSach[sh] = SafeDecrypt(rd["GhiChu"]?.ToString());
        //                }
        //            }

        //            var dictKhenThuong = new Dictionary<string, (string SoLuong, string GhiChu)>();
        //            using (var cn4 = new SqliteConnection($"Data Source={_csdl4Path}"))
        //            {
        //                cn4.Open();
        //                using (var cmdTao = new SqliteCommand("CREATE TABLE IF NOT EXISTS ThongKeCBCS_DuocKhenThuong (SoHieu TEXT, SoLuong_Khen TEXT, GhiChu_Khen TEXT);", cn4)) cmdTao.ExecuteNonQuery();

        //                using var cmd = new SqliteCommand("SELECT SoHieu, SoLuong_Khen, GhiChu_Khen FROM ThongKeCBCS_DuocKhenThuong", cn4);
        //                using var rd = cmd.ExecuteReader();
        //                while (rd.Read())
        //                {
        //                    string sh = SafeDecrypt(rd["SoHieu"]?.ToString());
        //                    if (!string.IsNullOrEmpty(sh)) dictKhenThuong[sh] = (rd["SoLuong_Khen"]?.ToString() ?? "0", rd["GhiChu_Khen"]?.ToString() ?? "");
        //                }
        //            }

        //            var dictDVKhen = new Dictionary<string, HashSet<string>>();
        //            using (var cn4 = new SqliteConnection($"Data Source={_csdl4Path}"))
        //            {
        //                cn4.Open();
        //                using (var cmdTao2 = new SqliteCommand("CREATE TABLE IF NOT EXISTS ThongKe_GiayKhen (SoHieu TEXT, DonVi_Khen TEXT);", cn4)) cmdTao2.ExecuteNonQuery();

        //                using var cmd = new SqliteCommand("SELECT SoHieu, DonVi_Khen FROM ThongKe_GiayKhen", cn4);
        //                using var rd = cmd.ExecuteReader();
        //                while (rd.Read())
        //                {
        //                    string sh = SafeDecrypt(rd["SoHieu"]?.ToString());
        //                    string dv = SafeDecrypt(rd["DonVi_Khen"]?.ToString());
        //                    if (!string.IsNullOrEmpty(sh) && !string.IsNullOrEmpty(dv))
        //                    {
        //                        if (!dictDVKhen.ContainsKey(sh)) dictDVKhen[sh] = new HashSet<string>();
        //                        dictDVKhen[sh].Add(dv);
        //                    }
        //                }
        //            }

        //            // ======================================================================
        //            // 🔥 THAY THẾ DATATABLE BẰNG LIST OBJECT (TỐI ƯU HIỆU SUẤT VIRTUAL MODE)
        //            // ======================================================================
        //            var listTemp = new List<ThongTinKhenThuongCBCS>(2000); // Khởi tạo trước dung lượng giảm lag GC

        //            using (var cn4 = new SqliteConnection($"Data Source={_csdl4Path}"))
        //            {
        //                cn4.Open();
        //                using var cmd = new SqliteCommand($"SELECT ID, HoVaTen, SoHieu, DonVi, TinhTrang FROM {tableThiDua}", cn4);
        //                using var rd = cmd.ExecuteReader();
        //                while (rd.Read())
        //                {
        //                    string sh = SafeDecrypt(rd["SoHieu"]?.ToString());
        //                    if (string.IsNullOrEmpty(sh)) continue;

        //                    string soLuongKhenStr = dictKhenThuong.ContainsKey(sh) ? dictKhenThuong[sh].SoLuong : "0";
        //                    int.TryParse(soLuongKhenStr, out int slKhen);

        //                    listTemp.Add(new ThongTinKhenThuongCBCS
        //                    {
        //                        ID = Convert.ToInt64(rd["ID"]),
        //                        HoVaTen = SafeDecrypt(rd["HoVaTen"]?.ToString()),
        //                        SoHieu = sh,
        //                        DonVi = SafeDecrypt(rd["DonVi"]?.ToString()),
        //                        TinhTrang = rd["TinhTrang"]?.ToString() ?? "",
        //                        SoLuong_Khen = slKhen,
        //                        GhiChu_Khen = dictKhenThuong.ContainsKey(sh) ? dictKhenThuong[sh].GhiChu : "",
        //                        GhiChu_DanhSach = dictDanhSach.ContainsKey(sh) ? dictDanhSach[sh] : "",
        //                        DanhSachDVKhen_An = dictDVKhen.ContainsKey(sh) ? string.Join(", ", dictDVKhen[sh]) : ""
        //                    });
        //                }
        //            }

        //            // --- GIỮ NGUYÊN CODE SẮP XẾP CHUẨN XÁC CỦA BẠN (Sắp xếp bằng LINQ siêu mượt) ---
        //            string[] thuTuUuTien = Module_DonVi.LayDanhSachDonViUuTienArray().Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        //            var uuTienMap = thuTuUuTien.Select((donvi, index) => new { donvi, index }).ToDictionary(x => x.donvi, x => x.index);

        //            return listTemp.OrderBy(r => uuTienMap.TryGetValue(r.DonVi ?? "", out int idx) ? idx : int.MaxValue)
        //                           .ThenBy(r => r.ID)
        //                           .ToList();
        //        });

        //        // 4. GÁN DỮ LIỆU ĐÃ CHUẨN BỊ XONG QUA BIẾN TOÀN CỤC TRÊN RAM
        //        _danhSachGoc = listNew;
        //        _danhSachHienThi = new List<ThongTinKhenThuongCBCS>(_danhSachGoc); // Clone sang danh sách hiển thị

        //        // 5. ĐỊNH DẠNG LẠI LƯỚI & CẬP NHẬT CÁC COMPONENT KHÁC
        //        DinhDangLuoi(); // Đảm bảo gọi hàm DinhDangLuoi mới (đã được bạn cấu hình tự add cột tay)

        //        // 🚀 BÁO CHO VIRTUAL MODE BIẾT CÓ BAO NHIÊU DÒNG ĐỂ LƯỚI VẼ THANH CUỘN
        //        kryptonDataGridView1_DanhSachCBCS.RowCount = _danhSachHienThi.Count;
        //        CapNhatComboBoxTuList();
        //        // Lưu ý: Hãy cập nhật logic đọc bên trong 2 hàm này đọc từ _danhSachHienThi thay cho DataTable
        //        CapNhatThongKeQuanSo();

        //        DaLoadDuLieu = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine("Lỗi tải Form 34: " + ex.Message);
        //        MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //    finally
        //    {
        //        // 6. MỞ KHÓA GIAO DIỆN (CHỈ VẼ LẠI ĐÚNG 1 LẦN DUY NHẤT)
        //        kryptonDataGridView1_DanhSachCBCS.ResumeLayout(true);
        //        SendMessage(kryptonDataGridView1_DanhSachCBCS.Handle, WM_SETREDRAW, 1, null);

        //        // 🔥 Yêu cầu lưới vẽ lại toàn bộ, lúc này Grid sẽ trigger liên tục sự kiện CellValueNeeded
        //        kryptonDataGridView1_DanhSachCBCS.Invalidate();

        //        if (toolStripProgressBar1_LamMoi != null)
        //            toolStripProgressBar1_LamMoi.Visible = false;
        //    }
        //}

        // Đặt biến này ở cấp độ Class (cùng chỗ với các biến private khác)
        // Mục đích: Khởi tạo Font 1 lần duy nhất, tránh rò rỉ bộ nhớ GDI+ (Memory Leak)
        public async Task ReloadDuLieu()
        {
            // =====================================================
            // CHỐNG RELOAD CHỒNG
            // =====================================================

            if (isUpdatingCombo)
                return;

            // =====================================================
            // CHỐNG FORM ĐÃ CHẾT
            // =====================================================

            if (IsDisposed || Disposing)
                return;

            try
            {
                // =====================================================
                // UI WAITING
                // =====================================================

                if (toolStripProgressBar1_LamMoi != null &&
                    !toolStripProgressBar1_LamMoi.IsDisposed)
                {
                    toolStripProgressBar1_LamMoi.Visible = true;
                    toolStripProgressBar1_LamMoi.Style = ProgressBarStyle.Marquee;
                }

                // =====================================================
                // VALIDATE GRID
                // =====================================================

                if (kryptonDataGridView1_DanhSachCBCS == null ||
                    kryptonDataGridView1_DanhSachCBCS.IsDisposed)
                {
                    return;
                }

                // =====================================================
                // FREEZE UI
                // =====================================================

                if (kryptonDataGridView1_DanhSachCBCS.IsHandleCreated)
                {
                    SendMessage(
                        kryptonDataGridView1_DanhSachCBCS.Handle,
                        WM_SETREDRAW,
                        0,
                        null);
                }

                kryptonDataGridView1_DanhSachCBCS.SuspendLayout();

                // =====================================================
                // LOAD BACKGROUND THREAD
                // =====================================================

                List<ThongTinKhenThuongCBCS> listNew =
                    await Task.Run(() =>
                    {
                        // =================================================
                        // CHỐNG FORM CHẾT GIỮA LUỒNG
                        // =================================================

                        if (IsDisposed || Disposing)
                            return new List<ThongTinKhenThuongCBCS>();

                        bool laTanBinh =
                            Module_TaiKhoan
                            .LayPhienBanPhanMem()
                            .Contains(
                                "tân binh",
                                StringComparison.OrdinalIgnoreCase);

                        string tableThiDua =
                            laTanBinh
                            ? "ThiDuaThang_TanBinh"
                            : "ThiDuaThang";

                        // =================================================
                        // LOAD DICTIONARY
                        // =================================================

                        var dictDanhSach =
                            new Dictionary<string, string>();

                        using (var cn2 =
                            new SqliteConnection(
                                $"Data Source={_csdl2Path}"))
                        {
                            cn2.Open();

                            using var cmd =
                                new SqliteCommand(
                                    "SELECT SoHieu, GhiChu FROM DanhSach",
                                    cn2);

                            using var rd = cmd.ExecuteReader();

                            while (rd.Read())
                            {
                                string sh =
                                    SafeDecrypt(
                                        rd["SoHieu"]?.ToString());

                                if (!string.IsNullOrEmpty(sh))
                                {
                                    dictDanhSach[sh] =
                                        SafeDecrypt(
                                            rd["GhiChu"]?.ToString());
                                }
                            }
                        }

                        var dictKhenThuong =
                            new Dictionary<string, (string SoLuong, string GhiChu)>();

                        using (var cn4 =
                            new SqliteConnection(
                                $"Data Source={_csdl4Path}"))
                        {
                            cn4.Open();

                            using (var cmdTao =
                                new SqliteCommand(
                                    "CREATE TABLE IF NOT EXISTS ThongKeCBCS_DuocKhenThuong (SoHieu TEXT, SoLuong_Khen TEXT, GhiChu_Khen TEXT);",
                                    cn4))
                            {
                                cmdTao.ExecuteNonQuery();
                            }

                            using var cmd =
                                new SqliteCommand(
                                    "SELECT SoHieu, SoLuong_Khen, GhiChu_Khen FROM ThongKeCBCS_DuocKhenThuong",
                                    cn4);

                            using var rd = cmd.ExecuteReader();

                            while (rd.Read())
                            {
                                string sh =
                                    SafeDecrypt(
                                        rd["SoHieu"]?.ToString());

                                if (!string.IsNullOrEmpty(sh))
                                {
                                    dictKhenThuong[sh] =
                                    (
                                        rd["SoLuong_Khen"]?.ToString() ?? "0",
                                        rd["GhiChu_Khen"]?.ToString() ?? ""
                                    );
                                }
                            }
                        }

                        var dictDVKhen =
                            new Dictionary<string, HashSet<string>>();

                        using (var cn4 =
                            new SqliteConnection(
                                $"Data Source={_csdl4Path}"))
                        {
                            cn4.Open();

                            using (var cmdTao2 =
                                new SqliteCommand(
                                    "CREATE TABLE IF NOT EXISTS ThongKe_GiayKhen (SoHieu TEXT, DonVi_Khen TEXT);",
                                    cn4))
                            {
                                cmdTao2.ExecuteNonQuery();
                            }

                            using var cmd =
                                new SqliteCommand(
                                    "SELECT SoHieu, DonVi_Khen FROM ThongKe_GiayKhen",
                                    cn4);

                            using var rd = cmd.ExecuteReader();

                            while (rd.Read())
                            {
                                string sh =
                                    SafeDecrypt(
                                        rd["SoHieu"]?.ToString());

                                string dv =
                                    SafeDecrypt(
                                        rd["DonVi_Khen"]?.ToString());

                                if (!string.IsNullOrEmpty(sh) &&
                                    !string.IsNullOrEmpty(dv))
                                {
                                    if (!dictDVKhen.ContainsKey(sh))
                                    {
                                        dictDVKhen[sh] =
                                            new HashSet<string>();
                                    }

                                    dictDVKhen[sh].Add(dv);
                                }
                            }
                        }

                        // =================================================
                        // BUILD LIST
                        // =================================================

                        var listTemp =
                            new List<ThongTinKhenThuongCBCS>(2000);

                        using (var cn4 =
                            new SqliteConnection(
                                $"Data Source={_csdl4Path}"))
                        {
                            cn4.Open();

                            using var cmd =
                                new SqliteCommand(
                                    $"SELECT ID, HoVaTen, SoHieu, DonVi, TinhTrang FROM {tableThiDua}",
                                    cn4);

                            using var rd = cmd.ExecuteReader();

                            while (rd.Read())
                            {
                                if (IsDisposed || Disposing)
                                    break;

                                string sh =
                                    SafeDecrypt(
                                        rd["SoHieu"]?.ToString());

                                if (string.IsNullOrEmpty(sh))
                                    continue;

                                string soLuongKhenStr =
                                    dictKhenThuong.ContainsKey(sh)
                                    ? dictKhenThuong[sh].SoLuong
                                    : "0";

                                int.TryParse(
                                    soLuongKhenStr,
                                    out int slKhen);

                                listTemp.Add(
                                    new ThongTinKhenThuongCBCS
                                    {
                                        ID = Convert.ToInt64(rd["ID"]),
                                        HoVaTen = SafeDecrypt(rd["HoVaTen"]?.ToString()),
                                        SoHieu = sh,
                                        DonVi = SafeDecrypt(rd["DonVi"]?.ToString()),
                                        TinhTrang = rd["TinhTrang"]?.ToString() ?? "",
                                        SoLuong_Khen = slKhen,
                                        GhiChu_Khen = dictKhenThuong.ContainsKey(sh)
                                            ? dictKhenThuong[sh].GhiChu
                                            : "",
                                        GhiChu_DanhSach = dictDanhSach.ContainsKey(sh)
                                            ? dictDanhSach[sh]
                                            : "",
                                        DanhSachDVKhen_An = dictDVKhen.ContainsKey(sh)
                                            ? string.Join(", ", dictDVKhen[sh])
                                            : ""
                                    });
                            }
                        }

                        // =================================================
                        // SORT
                        // =================================================

                        string[] thuTuUuTien =
                            Module_DonVi
                            .LayDanhSachDonViUuTienArray()
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToArray();

                        var uuTienMap =
                            thuTuUuTien
                            .Select((donvi, index) => new { donvi, index })
                            .ToDictionary(x => x.donvi, x => x.index);

                        return listTemp
                            .OrderBy(r =>
                                uuTienMap.TryGetValue(
                                    r.DonVi ?? "",
                                    out int idx)
                                    ? idx
                                    : int.MaxValue)
                            .ThenBy(r => r.ID)
                            .ToList();
                    });

                // =====================================================
                // FORM CÓ THỂ ĐÃ ĐÓNG SAU AWAIT
                // =====================================================

                if (IsDisposed || Disposing)
                    return;

                if (kryptonDataGridView1_DanhSachCBCS == null ||
                    kryptonDataGridView1_DanhSachCBCS.IsDisposed)
                {
                    return;
                }

                // =====================================================
                // GÁN RAM
                // =====================================================

                _danhSachGoc = listNew;

                _danhSachHienThi =
                    new List<ThongTinKhenThuongCBCS>(
                        _danhSachGoc);

                // =====================================================
                // UI UPDATE
                // =====================================================

                DinhDangLuoi();

                if (IsDisposed || Disposing)
                    return;

                kryptonDataGridView1_DanhSachCBCS.RowCount =
                    _danhSachHienThi.Count;

                // =====================================================
                // SAFE UI CALLS
                // =====================================================

                if (!IsDisposed && !Disposing)
                {
                    CapNhatComboBoxTuList();
                }

                if (!IsDisposed && !Disposing)
                {
                    CapNhatThongKeQuanSo();
                }

                DaLoadDuLieu = true;
            }
            catch (ObjectDisposedException)
            {
                // Form đã đóng -> bỏ qua an toàn
            }
            catch (InvalidOperationException)
            {
                // Handle/control đã bị hủy
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "Lỗi tải Form 34: " + ex);

                if (!IsDisposed && !Disposing)
                {
                    MessageBox.Show(
                        "Lỗi tải dữ liệu: " + ex.Message,
                        "Lỗi hệ thống",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            finally
            {
                try
                {
                    // =====================================================
                    // SAFE UNFREEZE
                    // =====================================================

                    if (kryptonDataGridView1_DanhSachCBCS != null &&
                        !kryptonDataGridView1_DanhSachCBCS.IsDisposed)
                    {
                        kryptonDataGridView1_DanhSachCBCS.ResumeLayout(true);

                        if (kryptonDataGridView1_DanhSachCBCS.IsHandleCreated)
                        {
                            SendMessage(
                                kryptonDataGridView1_DanhSachCBCS.Handle,
                                WM_SETREDRAW,
                                1,
                                null);
                        }

                        kryptonDataGridView1_DanhSachCBCS.Invalidate();
                    }

                    if (toolStripProgressBar1_LamMoi != null &&
                        !toolStripProgressBar1_LamMoi.IsDisposed)
                    {
                        toolStripProgressBar1_LamMoi.Visible = false;
                    }
                }
                catch
                {
                    // Không cho crash ở finally
                }
            }
        }
        private Font _fontBold;
        private void DinhDangLuoi()
        {
            var grid = kryptonDataGridView1_DanhSachCBCS;
            if (grid == null || grid.IsDisposed) return;

            grid.SuspendLayout();
            try
            {
                // 1. TỐI ƯU BỘ NHỚ VÀ ĐỒ HỌA (TRÁNH LEAK GDI VÀ CHỐNG NHÁY)
                _fontBold ??= new Font(grid.Font, FontStyle.Bold); // Khởi tạo 1 lần duy nhất

                // Bật DoubleBuffered qua Reflection để Grid cuộn mượt, không bị xé hình (Tear-tearing)
                typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                                    ?.SetValue(grid, true, null);

                // 2. CẤU HÌNH GIAO DIỆN HEADER VÀ MÀU SẮC
                grid.EnableHeadersVisualStyles = false;
                grid.BackgroundColor = Color.White;
                grid.BorderStyle = BorderStyle.None;

                grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                grid.ColumnHeadersHeight = grid.Font.Height + 25;
                grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 242, 249);
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(60, 60, 60);
                grid.ColumnHeadersDefaultCellStyle.Font = _fontBold; // Dùng Font đã Cache
                grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                // 3. TỐI ƯU HIỆU SUẤT TỐI ĐA (CHỐNG RENDER THỪA BÃI)
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None; // TẮT tính toán độ rộng toàn cục
                grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;       // TẮT tính toán chiều cao toàn cục
                grid.AllowUserToResizeRows = false;                              // Khóa resize dòng

                grid.RowTemplate.Height = grid.Font.Height + 10;
                grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
                grid.DefaultCellStyle.Padding = new Padding(2, 0, 2, 0);         // Giảm padding để paint nhẹ hơn

                grid.ReadOnly = true;
                grid.MultiSelect = false;                                        // Tránh logic tính toán chọn nhiều dòng
                grid.StandardTab = true;
                grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                grid.EditMode = DataGridViewEditMode.EditProgrammatically;
                grid.AllowUserToAddRows = false;
                grid.AllowUserToDeleteRows = false;
                grid.RowHeadersVisible = true;                                   // Vẫn giữ để vẽ số thứ tự
                grid.RowHeadersWidth = 50;

                // 4. KÍCH HOẠT VIRTUAL MODE VÀ DỌN DẸP AN TOÀN TRÁNH REDRAW KÉP
                grid.VirtualMode = true;
                if (grid.DataSource != null) grid.DataSource = null;
                if (grid.Columns.Count > 0) grid.Columns.Clear();

                // 5. TẠO CỘT THỦ CÔNG VÀ CHỈ ĐỊNH FILL ĐÚNG CHỖ
                // 🚀 AutoSizeMode được gán cục bộ: "HoVaTen" và "DonVi" sẽ dãn tự động lấp đầy màn hình
                // 5. TẠO CỘT THỦ CÔNG VÀ CHỈ ĐỊNH FILL ĐÚNG CHỖ
                var cauHinhCot = new[]
                {
    (Ten: "HoVaTen", TieuDe: "Họ và tên", Rong: 250, Fill: DataGridViewAutoSizeColumnMode.None, CanLe: DataGridViewContentAlignment.MiddleLeft, InDam: false, MauChu: Color.Empty),
    (Ten: "SoHieu", TieuDe: "Số hiệu", Rong: 120, Fill: DataGridViewAutoSizeColumnMode.None, CanLe: DataGridViewContentAlignment.MiddleCenter, InDam: false, MauChu: Color.Empty),
    (Ten: "DonVi", TieuDe: "Đơn vị", Rong: 200, Fill: DataGridViewAutoSizeColumnMode.None, CanLe: DataGridViewContentAlignment.MiddleLeft, InDam: false, MauChu: Color.Empty),
    (Ten: "TinhTrang", TieuDe: "Tình trạng", Rong: 140, Fill: DataGridViewAutoSizeColumnMode.None, CanLe: DataGridViewContentAlignment.MiddleCenter, InDam: false, MauChu: Color.Empty),
    (Ten: "SoLuong_Khen", TieuDe: "Tổng số khen thưởng", Rong: 160, Fill: DataGridViewAutoSizeColumnMode.None, CanLe: DataGridViewContentAlignment.MiddleCenter, InDam: true, MauChu: Color.DarkBlue),
    // ⭐ BỔ SUNG CỘT GHI CHÚ VÀO CUỐI CÙNG (Dùng Fill để lấp đầy màn hình)
    (Ten: "GhiChu_DanhSach", TieuDe: "Ghi chú", Rong: 250, Fill: DataGridViewAutoSizeColumnMode.Fill, CanLe: DataGridViewContentAlignment.MiddleLeft, InDam: false, MauChu: Color.DarkSlateGray)
};

                foreach (var cot in cauHinhCot)
                {
                    var col = new DataGridViewTextBoxColumn
                    {
                        Name = cot.Ten,
                        HeaderText = cot.TieuDe,
                        Width = cot.Rong,
                        AutoSizeMode = cot.Fill
                    };

                    col.DefaultCellStyle.Alignment = cot.CanLe;

                    // ⭐ BỔ SUNG AN TOÀN: Bật WrapMode cho riêng cột Ghi chú để chữ dài không bị khuất
                    if (cot.Ten == "GhiChu_DanhSach")
                    {
                        col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    }

                    if (cot.InDam || cot.MauChu != Color.Empty)
                    {
                        col.DefaultCellStyle.Font = cot.InDam ? _fontBold : grid.Font;
                        if (cot.MauChu != Color.Empty) col.DefaultCellStyle.ForeColor = cot.MauChu;
                    }
                    grid.Columns.Add(col);
                }

                // 6. GẮN SỰ KIỆN NẠP DỮ LIỆU ẢO
                grid.CellValueNeeded -= Grid_CellValueNeeded;
                grid.CellValueNeeded += Grid_CellValueNeeded;
            }
            finally
            {
                grid.ResumeLayout();
            }
        }
        // 🔥 SỰ KIỆN TRÁI TIM CỦA VIRTUAL MODE (Rất nhẹ, gọi hàng ngàn lần không lag)
        private void Grid_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _danhSachHienThi.Count) return;

            var cbcs = _danhSachHienThi[e.RowIndex];

            switch (kryptonDataGridView1_DanhSachCBCS.Columns[e.ColumnIndex].Name)
            {
                case "HoVaTen": e.Value = cbcs.HoVaTen; break;
                case "SoHieu": e.Value = cbcs.SoHieu; break;
                case "DonVi": e.Value = cbcs.DonVi; break;
                case "TinhTrang": e.Value = cbcs.TinhTrang; break;
                case "SoLuong_Khen": e.Value = cbcs.SoLuong_Khen; break;
                // ⭐ BỔ SUNG ÁNH XẠ DỮ LIỆU CỘT GHI CHÚ (An toàn: xử lý null)
                case "GhiChu_DanhSach": e.Value = cbcs.GhiChu_DanhSach ?? ""; break;
            }
        }

        // 🔥 HÀM TỰ VẼ ICON VÀO CỘT HỌ VÀ TÊN DỰA VÀO ĐIỀU KIỆN SỐ LƯỢNG KHEN THƯỞNG
        // 🔥 HÀM TỰ VẼ ICON VÀ CHỮ - ĐÃ XÓA SẠCH ĐƯỜNG KẺ VIỀN CỦA CỘT
        private void KryptonDataGridView1_DanhSachCBCS_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv == null) return;

            // Chỉ xử lý nếu đang vẽ ở cột "HoVaTen" và là dòng dữ liệu
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "HoVaTen")
            {
                // 1. MẤU CHỐT Ở ĐÂY: Chỉ vẽ Nền thường, Nền bôi đen và Focus. KHÔNG CÓ DataGridViewPaintParts.Border
                DataGridViewPaintParts parts = DataGridViewPaintParts.Background |
                                               DataGridViewPaintParts.SelectionBackground |
                                               DataGridViewPaintParts.Focus;
                e.Paint(e.CellBounds, parts);

                // 2. Lấy dữ liệu chữ
                string hoVaTenText = e.Value?.ToString() ?? "";

                // 3. Lấy số lượng khen để kiểm tra điều kiện vẽ icon
                int soLuongKhen = 0;
                if (dgv.Columns.Contains("SoLuong_Khen") && dgv.Rows[e.RowIndex].Cells["SoLuong_Khen"].Value != null)
                {
                    int.TryParse(dgv.Rows[e.RowIndex].Cells["SoLuong_Khen"].Value.ToString(), out soLuongKhen);
                }

                // 4. Tính toán Tọa độ (Pixel)
                int iconSize = 16;
                int paddingLeft = 6;  // Khoảng cách từ viền trái
                int paddingIconText = 6; // Khoảng cách giữa icon và chữ

                // Tọa độ Y căn giữa theo ô
                int yIcon = e.CellBounds.Y + (e.CellBounds.Height - iconSize) / 2;
                int xIcon = e.CellBounds.X + paddingLeft;

                // Vị trí mặc định của chữ nếu KHÔNG có icon
                int textStartX = xIcon;

                // 5. NẾU ĐẠT ĐIỀU KIỆN (>0) THÌ VẼ ICON VÀ DỜI CHỮ SANG PHẢI
                if (soLuongKhen > 0 && _iconKhenThuong != null)
                {
                    e.Graphics.DrawImage(_iconKhenThuong, new Rectangle(xIcon, yIcon, iconSize, iconSize));
                    textStartX = xIcon + iconSize + paddingIconText;
                }

                // 6. Vẽ chữ (Họ và Tên)
                if (!string.IsNullOrEmpty(hoVaTenText))
                {
                    Rectangle textBounds = new Rectangle(
                        textStartX,
                        e.CellBounds.Y,
                        e.CellBounds.Width - (textStartX - e.CellBounds.X),
                        e.CellBounds.Height);

                    // Lấy màu chữ tương ứng với trạng thái (chọn hoặc không chọn)
                    Color textColor = (e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected
                                      ? e.CellStyle.SelectionForeColor
                                      : e.CellStyle.ForeColor;

                    // TextFormatFlags để vẽ chữ căn giữa dọc, trái ngang, tự cắt nếu dài
                    TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;

                    TextRenderer.DrawText(e.Graphics, hoVaTenText, e.CellStyle.Font, textBounds, textColor, flags);
                }

                // Báo cho lưới biết mình đã vẽ xong hết rồi, đừng tự động vẽ đè bất cứ cái gì (kể cả viền) lên nữa
                e.Handled = true;
            }
        }
        private void LoadComboBoxTinhTrang()
        {
            if (comboBox_TinhTrangCongTac == null) return;

            isUpdatingCombo = true;
            comboBox_TinhTrangCongTac.Items.Clear();
            comboBox_TinhTrangCongTac.Items.Add("Tất cả");
            comboBox_TinhTrangCongTac.Items.Add("Đang công tác");
            comboBox_TinhTrangCongTac.Items.Add("Chuyển công tác");
            comboBox_TinhTrangCongTac.SelectedIndex = 0;
            isUpdatingCombo = false;
        }
        private void KryptonDataGridView1_DanhSachCBCS_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            // Lấy số thứ tự (index + 1)
            string rowIdx = (e.RowIndex + 1).ToString();

            // Căn giữa số thứ tự
            var centerFormat = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);

            // Vẽ chữ số lên cột RowHeader
            e.Graphics.DrawString(rowIdx, grid.Font, SystemBrushes.ControlText, headerBounds, centerFormat);
        }
        // Sự kiện Click vào Grid đổ dữ liệu xuống TextBox
        private void KryptonDataGridView1_DanhSachCBCS_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _danhSachHienThi.Count)
            {
                var cbcs = _danhSachHienThi[e.RowIndex];

                if (kryptonTextBox1_STT != null) kryptonTextBox1_STT.Text = (e.RowIndex + 1).ToString();
                if (kryptonTextBox1_HoVaTen != null) kryptonTextBox1_HoVaTen.Text = cbcs.HoVaTen;
                if (kryptonTextBox1_SoHieu != null) kryptonTextBox1_SoHieu.Text = cbcs.SoHieu;
                if (kryptonTextBox1_DonVi != null) kryptonTextBox1_DonVi.Text = cbcs.DonVi;
                if (kryptonTextBox1_TinhTrang != null) kryptonTextBox1_TinhTrang.Text = cbcs.TinhTrang;
                if (kryptonTextBox1_SoLuong != null) kryptonTextBox1_SoLuong.Text = cbcs.SoLuong_Khen.ToString();
            }
        }
        /// <summary>
        /// Hàm đếm và cập nhật thống kê trên StatusStrip dựa vào dữ liệu đang hiển thị trên lưới
        /// </summary>
        public void CapNhatThongKeQuanSo()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(CapNhatThongKeQuanSo)); return; }

            if (_danhSachHienThi != null)
            {
                int tongCong = _danhSachHienThi.Count(x => !string.IsNullOrWhiteSpace(x.SoHieu));
                int dangCongTac = _danhSachHienThi.Count(x => string.Equals(x.TinhTrang, "Đang công tác", StringComparison.OrdinalIgnoreCase));
                int chuyenCongTac = _danhSachHienThi.Count(x => string.Equals(x.TinhTrang, "Chuyển công tác", StringComparison.OrdinalIgnoreCase));
                int soNguoiDuocKhen = _danhSachHienThi.Count(x => x.SoLuong_Khen > 0);

                if (toolStripStatusLabel1 != null) toolStripStatusLabel1.Text = $"Tổng cộng: {tongCong} đồng chí";
                if (toolStripStatusLabel2 != null) toolStripStatusLabel2.Text = $"Đang công tác: {dangCongTac} đồng chí";
                if (toolStripStatusLabel3 != null) toolStripStatusLabel3.Text = $"Chuyển công tác: {chuyenCongTac} đồng chí";
                if (toolStripStatusLabel4 != null) toolStripStatusLabel4.Text = $"Tổng số CBCS đã được khen thưởng: {soNguoiDuocKhen} đồng chí";
            }
        }
        private void CauHinhStatusStrip()
        {
            // MẶC ĐỊNH ẨN PROGRESS BAR KHI MỚI LOAD FORM
            if (toolStripProgressBar1_LamMoi != null)
            {
                toolStripProgressBar1_LamMoi.Visible = false;
            }

            // Bật Spring (đẩy lò xo) để Label 2 và 3 tự động chiếm không gian trống ở giữa
            if (toolStripStatusLabel2 != null)
            {
                toolStripStatusLabel2.Spring = true;
                toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleCenter;
            }

            if (toolStripStatusLabel3 != null)
            {
                toolStripStatusLabel3.Spring = true;
                toolStripStatusLabel3.TextAlign = ContentAlignment.MiddleCenter;
            }

            // Label 1 và 4 không dùng Spring, sẽ nằm gọn gàng ở 2 đầu
            if (toolStripStatusLabel1 != null)
                toolStripStatusLabel1.TextAlign = ContentAlignment.MiddleLeft;

            if (toolStripStatusLabel4 != null)
                toolStripStatusLabel4.TextAlign = ContentAlignment.MiddleLeft;
        }
        // =========================================================
        // Sự kiện Click đúp vào lưới để mở thẳng Form 37
        // =========================================================
        private void KryptonDataGridView1_DanhSachCBCS_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // Đảm bảo người dùng click vào một dòng hợp lệ (không phải click vào Header)
            if (e.RowIndex >= 0)
            {
                // 1. Vì khi click đúp, sự kiện CellClick đã chạy trước đó rồi (đổ dữ liệu xuống TextBox)
                // Nên lúc này trên TextBox đã có đầy đủ dữ liệu. Ta chỉ việc gọi hàm của Nút bấm chạy là xong!

                kryptonButton_GoiFromChiTietKhenThuong_Click(sender, e);
            }
        }
        private void kryptonButton_GoiFromChiTietKhenThuong_Click(object sender, EventArgs e)
        {
            // 1. Lấy dữ liệu từ TextBox trên Form 36
            string hoTen = kryptonTextBox1_HoVaTen?.Text ?? "";
            string soHieu = kryptonTextBox1_SoHieu?.Text ?? "";
            string donVi = kryptonTextBox1_DonVi?.Text ?? "";
            string tinhTrang = kryptonTextBox1_TinhTrang?.Text ?? "";
            // Kiểm tra xem người dùng đã chọn ai trên lưới chưa
            if (string.IsNullOrWhiteSpace(soHieu))
            {
                MessageBox.Show("Vui lòng chọn một Cán bộ chiến sĩ từ danh sách trước khi xem chi tiết!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // 2. Tìm PanelContainer và quản lý Form 37 trên RAM
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha == null) return;

            var panel = formCha.Controls.Find("PanelContainer", true).FirstOrDefault() as Panel;
            if (panel == null) return;

            var form36 = this; // Chính là Form 36 hiện tại

            // Ẩn tất cả Form đang có
            foreach (Control ctl in panel.Controls)
            {
                if (ctl is Form frm) frm.Hide();
            }

            // Tìm Form 37 trong RAM
            var form37 = panel.Controls.OfType<Form35_ChiTietKhenThuong>().FirstOrDefault();

            if (form37 == null)
            {
                // Nếu chưa có -> Tạo mới
                form37 = new Form35_ChiTietKhenThuong
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill
                };

                // Khi Form 37 đóng, gọi lại Form 36 và làm mới dữ liệu
                form37.FormClosed += (s, ev) =>
                {
                    if (panel.IsDisposed) return;
                    if (!form36.IsDisposed)
                    {
                        form36.Show();
                        form36.BringToFront();
                        form36.ReloadDuLieu(); // Load lại để cập nhật số lượng giấy khen
                    }
                };

                panel.Controls.Add(form37);
            }

            // 3. 🚀 ĐIỂM MẤU CHỐT: Truyền tín hiệu sang Form 37
            // Gọi hàm NhanDuLieuTuForm36 (chúng ta sẽ tạo nó ở Phần 2)
            form37.NhanDuLieuTuForm36(hoTen, soHieu, donVi, tinhTrang);
            // 4. Hiển thị Form 37
            form37.Show();
            form37.BringToFront();
        }
        private void kryptonButton_LamMoiCacOTimKiem_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Bật cờ chặn sự kiện BoLoc_ValueChanged để tránh tính toán thừa
                isUpdatingCombo = true;

                // 2. Reset các điều khiển nhập liệu về trạng thái ban đầu
                if (textBox_TimKiemTheoTen != null)
                    textBox_TimKiemTheoTen.Clear();

                if (comboBox_TimKiemDonVi != null && comboBox_TimKiemDonVi.Items.Count > 0)
                    comboBox_TimKiemDonVi.SelectedIndex = 0; // Về "Tất cả"

                if (comboBox_TinhTrangCongTac != null && comboBox_TinhTrangCongTac.Items.Count > 0)
                    comboBox_TinhTrangCongTac.SelectedIndex = 0; // Về "Tất cả"

                if (comboBox1_DonViKhenThuong != null && comboBox1_DonViKhenThuong.Items.Count > 0)
                    comboBox1_DonViKhenThuong.SelectedIndex = 0; // Về "Tất cả"

                // 3. Tắt cờ chặn để các logic khác hoạt động bình thường
                isUpdatingCombo = false;

                // 4. Xóa bỏ hoàn toàn bộ lọc trên DataView để hiển thị toàn bộ danh sách
                // Tìm đoạn: if (dtDanhSachHienThi != null) { dtDanhSachHienThi.DefaultView.RowFilter = ""; }
                // THAY THẾ BẰNG:

                if (_danhSachGoc != null)
                {
                    _danhSachHienThi = new List<ThongTinKhenThuongCBCS>(_danhSachGoc);
                    if (kryptonDataGridView1_DanhSachCBCS != null)
                    {
                        kryptonDataGridView1_DanhSachCBCS.RowCount = _danhSachHienThi.Count;
                        kryptonDataGridView1_DanhSachCBCS.Invalidate();
                    }
                }

                // 5. Cập nhật lại các con số thống kê ở StatusStrip
                CapNhatThongKeQuanSo();

                // 6. Tùy chọn: Xóa trắng các TextBox hiển thị thông tin chi tiết bên dưới 
                // (Để tránh việc người dùng thấy thông tin của người cũ trong khi chưa chọn người mới)
                ClearCacTextBoxChiTiet();

                // 7. Reset vị trí chọn trên lưới (Bỏ chọn dòng hiện tại)
                if (kryptonDataGridView1_DanhSachCBCS != null)
                {
                    kryptonDataGridView1_DanhSachCBCS.ClearSelection();
                    kryptonDataGridView1_DanhSachCBCS.CurrentCell = null;
                }
                // Focus con trỏ chuột trở lại ô tìm kiếm để người dùng gõ tiếp luôn
                if (textBox_TimKiemTheoTen != null)
                {
                    textBox_TimKiemTheoTen.Focus();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi xóa bộ lọc: " + ex.Message);
            }
        }
        // Hàm hỗ trợ xóa sạch dữ liệu trên các TextBox chi tiết cho gọn giao diện
        private void ClearCacTextBoxChiTiet()
        {
            if (kryptonTextBox1_STT != null) kryptonTextBox1_STT.Clear();
            if (kryptonTextBox1_HoVaTen != null) kryptonTextBox1_HoVaTen.Clear();
            if (kryptonTextBox1_SoHieu != null) kryptonTextBox1_SoHieu.Clear();
            if (kryptonTextBox1_DonVi != null) kryptonTextBox1_DonVi.Clear();
            if (kryptonTextBox1_TinhTrang != null) kryptonTextBox1_TinhTrang.Clear();
            if (kryptonTextBox1_SoLuong != null) kryptonTextBox1_SoLuong.Clear();
        }
        private async void lamMoi_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Làm mới
            // 1. Khóa nút lại để tránh bấm nhiều lần
            var btn = sender as Control;
            if (btn != null) btn.Enabled = false;

            // 2. Hiện Progress Bar lên và cho chạy lướt (Marquee)
            if (toolStripProgressBar1_LamMoi != null)
            {
                toolStripProgressBar1_LamMoi.Style = ProgressBarStyle.Marquee;
                toolStripProgressBar1_LamMoi.MarqueeAnimationSpeed = 30;
                toolStripProgressBar1_LamMoi.Visible = true; // HIỆN LÊN CHỈ KHI BẤM NÚT
            }

            // Reset giao diện tìm kiếm
            if (textBox_TimKiemTheoTen != null) textBox_TimKiemTheoTen.Clear();
            if (comboBox_TimKiemDonVi != null && comboBox_TimKiemDonVi.Items.Count > 0) comboBox_TimKiemDonVi.SelectedIndex = 0;
            if (comboBox_TinhTrangCongTac != null && comboBox_TinhTrangCongTac.Items.Count > 0) comboBox_TinhTrangCongTac.SelectedIndex = 0;

            // Chờ 400ms để tạo hiệu ứng "đang xử lý"
            await Task.Delay(400);
            // 3. Load lại dữ liệu từ DB (Bên trong hàm này đã gọi sẵn CapNhatThongKeQuanSo)
            await ReloadDuLieu();
            // 4. Kết thúc: Làm đầy thanh Progress Bar 100% cho đẹp rồi giấu nó đi
            if (toolStripProgressBar1_LamMoi != null)
            {
                toolStripProgressBar1_LamMoi.Style = ProgressBarStyle.Blocks;
                toolStripProgressBar1_LamMoi.Value = 100;
                await Task.Delay(200); // Lưu lại 100% khoảng 0.2s cho đẹp mắt

                toolStripProgressBar1_LamMoi.Value = 0; // Trả về 0 trước khi ẩn
                toolStripProgressBar1_LamMoi.Visible = false; // ẨN ĐI TRẢ LẠI GIAO DIỆN CŨ
            }
            // 5. Mở khóa lại nút bấm
            if (btn != null) btn.Enabled = true;
        }
        // =========================================================
        // XỬ LÝ CLICK CHUỘT PHẢI TỰ ĐỘNG CHỌN DÒNG
        // =========================================================
        private void KryptonDataGridView1_DanhSachCBCS_MouseDown(object sender, MouseEventArgs e)
        {
            // Chỉ xử lý nếu người dùng nhấn chuột phải
            if (e.Button == MouseButtons.Right)
            {
                var grid = sender as DataGridView;
                if (grid == null) return;

                // Xác định tọa độ chuột đang nằm trên Cell nào
                var hit = grid.HitTest(e.X, e.Y);

                // Nếu chuột nằm trên một dòng hợp lệ (bỏ qua Header)
                if (hit.RowIndex >= 0)
                {
                    // Xóa các dòng đang bôi xanh cũ
                    grid.ClearSelection();

                    // Bôi xanh dòng mới nơi chuột phải vừa click
                    grid.Rows[hit.RowIndex].Selected = true;

                    // Cập nhật CurrentCell để đồng bộ UI
                    grid.CurrentCell = grid.Rows[hit.RowIndex].Cells[hit.ColumnIndex >= 0 ? hit.ColumnIndex : 0];

                    // Kích hoạt hàm CellClick để dữ liệu từ lưới được đổ xuống các TextBox bên dưới 
                    // (Giống hệt như khi người dùng Click chuột trái)
                    KryptonDataGridView1_DanhSachCBCS_CellClick(grid, new DataGridViewCellEventArgs(hit.ColumnIndex, hit.RowIndex));
                }
            }
        }
        private void xoaTimKiem_ToolStripMenuItem_Click(object sender, EventArgs e) => kryptonButton_LamMoiCacOTimKiem.PerformClick();
        private void capNhatThongTinKhenThuong_ToolStripMenuItem_Click(object sender, EventArgs e) => kryptonButton_GoiFromChiTietKhenThuong.PerformClick();
        // =========================================================
        // HIỆU ỨNG UI: VIỀN XANH ĐẬM KHI HOVER VÀ FOCUS VÀO TEXTBOX
        // =========================================================
        private void ĐangKyHieuUngVienTextBox()
        {
            // Danh sách các TextBox cần tạo hiệu ứng
            var danhSachTextBox = new List<Control>
            {
                textBox_TimKiemTheoTen, // Có thể là TextBox chuẩn của Windows
                kryptonTextBox1_STT,
                kryptonTextBox1_HoVaTen,
                kryptonTextBox1_SoHieu,
                kryptonTextBox1_DonVi,
                kryptonTextBox1_TinhTrang,
                kryptonTextBox1_SoLuong
            };

            // Màu sắc quy định (Xanh dương đậm cho Focus, Xanh dương nhạt cho Hover)
            Color mauFocus = Color.FromArgb(0, 120, 215); // Xanh Windows 10
            Color mauHover = Color.FromArgb(100, 180, 255);
            Color mauMacDinh = Color.FromArgb(180, 180, 180); // Xám viền mặc định

            foreach (var control in danhSachTextBox)
            {
                if (control == null) continue;

                // XỬ LÝ CHO KRYPTON TEXTBOX
                if (control is Krypton.Toolkit.KryptonTextBox kTextbox)
                {
                    // Thiết lập viền mặc định bo góc nhẹ cho đẹp
                    kTextbox.StateCommon.Border.Rounding = 3;
                    kTextbox.StateCommon.Border.Width = 1;
                    kTextbox.StateCommon.Border.Color1 = mauMacDinh;

                    // 1. Khi chuột lướt qua (Hover)
                    kTextbox.MouseEnter += (s, e) =>
                    {
                        if (!kTextbox.Focused) // Chỉ đổi màu hover nếu chưa được focus
                        {
                            kTextbox.StateCommon.Border.Color1 = mauHover;
                            kTextbox.StateCommon.Border.Width = 1;
                        }
                    };

                    // 2. Khi chuột đi ra khỏi
                    kTextbox.MouseLeave += (s, e) =>
                    {
                        if (!kTextbox.Focused) // Trả về mặc định nếu không giữ focus
                        {
                            kTextbox.StateCommon.Border.Color1 = mauMacDinh;
                            kTextbox.StateCommon.Border.Width = 1;
                        }
                    };

                    // 3. Khi nháy trỏ chuột vào trong (Focus)
                    kTextbox.Enter += (s, e) =>
                    {
                        kTextbox.StateCommon.Border.Color1 = mauFocus;
                        kTextbox.StateCommon.Border.Width = 2; // Viền dày hơn một chút khi gõ
                    };

                    // 4. Khi bấm ra chỗ khác (Mất Focus)
                    kTextbox.Leave += (s, e) =>
                    {
                        kTextbox.StateCommon.Border.Color1 = mauMacDinh;
                        kTextbox.StateCommon.Border.Width = 1;
                    };
                }
                // XỬ LÝ CHO TEXTBOX MẶC ĐỊNH CỦA WINDOWS (Nếu textBox_TimKiemTheoTen không phải Krypton)
                else if (control is System.Windows.Forms.TextBox stdTextbox)
                {
                    // TextBox chuẩn của Windows không hỗ trợ đổi màu viền trực tiếp.
                    // Cách mượt và không giật lag nhất là đổi màu nền (BackColor) khi Focus.
                    Color nenFocus = Color.FromArgb(240, 248, 255); // Xanh nhạt
                    Color nenMacDinh = Color.White;

                    stdTextbox.Enter += (s, e) => stdTextbox.BackColor = nenFocus;
                    stdTextbox.Leave += (s, e) => stdTextbox.BackColor = nenMacDinh;
                }
            }
        }
        /// <summary>
        /// Hàm phân tích chi tiết khen thưởng dựa trên dữ liệu đang hiển thị trên DataGridView (đã qua bộ lọc)
        /// </summary>
        public void HienThiPhanTichKhenThuong()
        {
            if (_danhSachHienThi == null || _danhSachHienThi.Count == 0)
            {
                MessageBox.Show("Hiện không có dữ liệu nào trên danh sách để phân tích!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int tongSoGiayKhen = 0;
            var thongKeDonVi = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            char[] separator = new char[] { ',' };

            foreach (var cbcs in _danhSachHienThi) // Đã sửa thành đọc từ List
            {
                if (string.IsNullOrWhiteSpace(cbcs.SoHieu)) continue;

                tongSoGiayKhen += cbcs.SoLuong_Khen;
                string chuoiDonVi = cbcs.DanhSachDVKhen_An; // Đọc trực tiếp thuộc tính

                if (!string.IsNullOrWhiteSpace(chuoiDonVi))
                {
                    string[] danhSachDV = chuoiDonVi.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < danhSachDV.Length; i++)
                    {
                        string dv = danhSachDV[i].Trim();
                        if (dv.Length > 0)
                        {
                            if (thongKeDonVi.TryGetValue(dv, out int count)) thongKeDonVi[dv] = count + 1;
                            else thongKeDonVi[dv] = 1;
                        }
                    }
                }
            }

            var sb = new StringBuilder()
                .AppendLine("KẾT QUẢ PHÂN TÍCH KHEN THƯỞNG")
                .AppendLine("(Dựa trên danh sách đang hiển thị hiện tại)")
                .AppendLine("--------------------------------------------------")
                .AppendLine($"- Tổng số lượng Giấy khen / Bằng khen: {tongSoGiayKhen}")
                .AppendLine($"- Có tổng cộng {thongKeDonVi.Count} đơn vị ra quyết định khen thưởng.")
                .AppendLine("--------------------------------------------------")
                .AppendLine("🏆 CHI TIẾT:");

            foreach (var item in thongKeDonVi.OrderByDescending(x => x.Value))
            {
                sb.AppendLine($"+ {item.Key}: {item.Value} lượt");
            }

            MessageBox.Show(sb.ToString(), "Báo cáo phân tích", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void phanTichKhenThuong_ToolStripMenuItem_Click(object sender, EventArgs e)
            => HienThiPhanTichKhenThuong();
        private async void thongKeKhenThuong_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_danhSachHienThi == null || _danhSachHienThi.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để phân tích.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Cursor.Current = Cursors.WaitCursor;
            try
            {
                var thongKeDonVi = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                char[] separator = { ',' };

                foreach (var cbcs in _danhSachHienThi) // Đã sửa
                {
                    if (string.IsNullOrWhiteSpace(cbcs.SoHieu)) continue;
                    if (string.IsNullOrWhiteSpace(cbcs.DanhSachDVKhen_An)) continue;

                    string hoTen = string.IsNullOrWhiteSpace(cbcs.HoVaTen) ? "Không rõ" : cbcs.HoVaTen.Trim();
                    string donViCBCS = cbcs.DonVi?.Trim();
                    string thongTin = string.IsNullOrWhiteSpace(donViCBCS) ? hoTen : $"{hoTen} ({donViCBCS})";

                    string[] danhSachDV = cbcs.DanhSachDVKhen_An.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var dvRaw in danhSachDV)
                    {
                        string dv = dvRaw.Trim();
                        if (string.IsNullOrEmpty(dv)) continue;

                        if (!thongKeDonVi.TryGetValue(dv, out var list))
                        {
                            list = new List<string>();
                            thongKeDonVi[dv] = list;
                        }
                        list.Add(thongTin);
                    }
                }

                int tongSoLuotThucTe = thongKeDonVi.Values.Sum(x => x.Distinct().Count());

                using var sfd = new SaveFileDialog
                {
                    Title = "Lưu báo cáo thống kê",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx|Text File (*.txt)|*.txt",
                    FileName = $"ThongKe_DonVi_Khen_{DateTime.Now:ddMMyyyy_HHmm}"
                };

                if (sfd.ShowDialog() != DialogResult.OK) return;

                string filePath = sfd.FileName;
                string ext = Path.GetExtension(filePath).ToLower();

                if (ext == ".xlsx")
                {
                    using var wb = new ClosedXML.Excel.XLWorkbook();
                    var ws = wb.Worksheets.Add("Thống kê đơn vị");

                    ws.Style.Font.FontName = "Times New Roman";
                    ws.Style.Font.FontSize = 13;

                    var rangeHeader = ws.Range("A1:D1").Merge();
                    rangeHeader.Value = "BÁO CÁO PHÂN TÍCH LƯỢT KHEN THƯỞNG THEO ĐƠN VỊ TẶNG";
                    rangeHeader.Style.Font.Bold = true;
                    rangeHeader.Style.Font.FontSize = 15;
                    rangeHeader.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                    ws.Cell("A2").Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                    ws.Range("A2:D2").Merge().Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                    int headerRow = 4;
                    ws.Row(headerRow).Height = 35;

                    string[] columns = { "STT", "Đơn vị ra quyết định khen thưởng", "Số lượt", "Danh sách CBCS được khen" };
                    for (int i = 0; i < columns.Length; i++)
                    {
                        var cell = ws.Cell(headerRow, i + 1);
                        cell.Value = columns[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#E8E8E8");
                        cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                        cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                    }

                    int currentRow = headerRow + 1;
                    int sttIdx = 1;

                    foreach (var item in thongKeDonVi.OrderByDescending(x => x.Value.Distinct().Count()))
                    {
                        var dsNguoiKhen = item.Value.Distinct().ToList();

                        ws.Cell(currentRow, 1).Value = sttIdx++;
                        ws.Cell(currentRow, 2).Value = item.Key;
                        ws.Cell(currentRow, 3).Value = dsNguoiKhen.Count;
                        ws.Cell(currentRow, 4).Value = string.Join("; ", dsNguoiKhen);

                        ws.Cell(currentRow, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                        ws.Cell(currentRow, 2).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;
                        ws.Cell(currentRow, 3).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                        ws.Cell(currentRow, 4).Style.Alignment.WrapText = true;

                        currentRow++;
                    }

                    var totalRange = ws.Range(currentRow, 1, currentRow, 2).Merge();
                    totalRange.Value = $"Tổng cộng: {thongKeDonVi.Count} đơn vị";
                    totalRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;
                    totalRange.Style.Font.Italic = true;

                    ws.Cell(currentRow, 3).Value = tongSoLuotThucTe;
                    ws.Cell(currentRow, 3).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                    var rowFooter = ws.Range(currentRow, 1, currentRow, 4);
                    rowFooter.Style.Font.Bold = true;
                    rowFooter.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#F9F9F9");

                    var fullTableRange = ws.Range(headerRow, 1, currentRow, 4);
                    fullTableRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    fullTableRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

                    ws.Column(1).Width = 8;
                    ws.Column(2).Width = 45;
                    ws.Column(3).Width = 15;
                    ws.Column(4).Width = 60;

                    wb.SaveAs(filePath);
                }
                else
                {
                    var sb = new StringBuilder()
                        .AppendLine("==================================================")
                        .AppendLine("    BÁO CÁO THỐNG KÊ LƯỢT KHEN THƯỞNG")
                        .AppendLine($"    Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .AppendLine("==================================================")
                        .AppendLine($"Tổng số lượt khen thưởng ghi nhận: {tongSoLuotThucTe}")
                        .AppendLine($"Tổng số cơ quan khen thưởng: {thongKeDonVi.Count}")
                        .AppendLine("--------------------------------------------------\n");

                    foreach (var item in thongKeDonVi.OrderByDescending(x => x.Value.Distinct().Count()))
                    {
                        var dsNguoiKhen = item.Value.Distinct().ToList();
                        sb.AppendLine($"[+] {item.Key.ToUpper()}: {dsNguoiKhen.Count} lượt");
                        sb.AppendLine($"    - CBCS: {string.Join(", ", dsNguoiKhen)}");
                        sb.AppendLine();
                    }

                    System.IO.File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                }

                if (MessageBox.Show("Đã phân tích và xuất báo cáo thành công!\nBạn có muốn mở tệp ngay không?", "Thành công", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
                }
            }
            catch (System.IO.IOException)
            {
                MessageBox.Show("Lỗi lưu file: File Excel có thể đang được mở ở một cửa sổ khác. Vui lòng đóng và thử lại.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi thực thi: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }
        private void xuatDuLieuRaTepExcel_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_danhSachHienThi == null || _danhSachHienThi.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu nào để xuất ra Excel!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel Workbook (*.xlsx)|*.xlsx";
                sfd.Title = "Lưu file Thống kê khen thưởng";
                sfd.FileName = $"ThongKeKhenThuong_{DateTime.Now:ddMMyyyy_HHmm}.xlsx";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    Cursor.Current = Cursors.WaitCursor;
                    try
                    {
                        using (var workbook = new ClosedXML.Excel.XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("DS Khen Thuong");

                            // Tìm mảng headers và thêm "Ghi chú" vào cuối
                            string[] headers = { "STT", "Họ và tên", "Số hiệu", "Đơn vị", "Tình trạng công tác", "Tổng số lần khen thưởng", "Ghi chú" };
                            for (int i = 0; i < headers.Length; i++)
                            {
                                var cell = worksheet.Cell(1, i + 1);
                                cell.Value = headers[i];
                                cell.Style.Font.Bold = true;
                                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.AliceBlue;
                                cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                                cell.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                            }

                            int rowIndex = 2;
                            int stt = 1;
                            foreach (var cbcs in _danhSachHienThi) // Đã sửa đọc từ mảng RAM
                            {
                                if (string.IsNullOrWhiteSpace(cbcs.SoHieu)) continue;

                                worksheet.Cell(rowIndex, 1).Value = stt++;
                                worksheet.Cell(rowIndex, 2).Value = cbcs.HoVaTen;
                                worksheet.Cell(rowIndex, 3).Value = cbcs.SoHieu;
                                worksheet.Cell(rowIndex, 4).Value = cbcs.DonVi;
                                worksheet.Cell(rowIndex, 5).Value = cbcs.TinhTrang;
                                worksheet.Cell(rowIndex, 6).Value = cbcs.SoLuong_Khen;
                                // ⭐ BỔ SUNG DÒNG GHI EXCEL CHO GHI CHÚ (Cột số 7)
                                worksheet.Cell(rowIndex, 7).Value = cbcs.GhiChu_DanhSach;

                                for (int i = 1; i <= headers.Length; i++)
                                {
                                    worksheet.Cell(rowIndex, i).Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                                }
                                rowIndex++;
                            }

                            worksheet.Columns().AdjustToContents();

                            // 🔥 GỌI MODULE ĐÓNG DẤU BẢN QUYỀN TẠI ĐÂY (TRƯỚC KHI LƯU) 🔥
                            Module_BanQuyen.DongDauExcel(workbook);
                            workbook.SaveAs(sfd.FileName);
                        }

                        DialogResult openFile = MessageBox.Show("Xuất dữ liệu thành công! Bạn có muốn mở file Excel vừa tạo lên không?", "Hoàn tất", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (openFile == DialogResult.Yes)
                        {
                            Process.Start(new ProcessStartInfo { FileName = sfd.FileName, UseShellExecute = true });
                        }
                    }
                    catch (System.IO.IOException)
                    {
                        MessageBox.Show("Không thể lưu file.\nVui lòng kiểm tra xem file Excel này có đang được mở ở một cửa sổ khác không, hãy đóng nó lại và thử lại!", "Lỗi lưu tệp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Quá trình xuất dữ liệu gặp lỗi:\n{ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        Cursor.Current = Cursors.Default;
                    }
                }
            }
        }
        private async void xoaToanBoDuLieu_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 1. KIỂM TRA
            string dbPath = _csdl4Path;
            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
            {
                MessageBox.Show("Không tìm thấy CSDL khen thưởng (csdl4.db)!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (kryptonDataGridView1_DanhSachCBCS.Rows.Count == 0)
            {
                MessageBox.Show("Hiện không có dữ liệu để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 2. XÁC MINH ADMIN
            DialogResult kqAdmin;
            using (Form24_XacMinhAdmin frm = new Form24_XacMinhAdmin())
            {
                frm.TopMost = true;
                frm.StartPosition = FormStartPosition.CenterScreen;
                kqAdmin = frm.ShowDialog(this);
            }
            if (kqAdmin != DialogResult.OK) return;

            // 3. BẮT ĐẦU XÓA
            Cursor.Current = Cursors.WaitCursor;
            kryptonDataGridView1_DanhSachCBCS.DataSource = null; // Ép gỡ lưới để nhả file DB

            try
            {
                // 🔥 TUYỆT KỸ: BẮT TASK TRẢ VỀ 2 CON SỐ CÙNG LÚC DÙNG TUPLE (int, int)
                var ketQuaDem = await Task.Run(() =>
                {
                    int demThongKe = 0;
                    int demChiTiet = 0;

                    using (var conn = new SqliteConnection($"Data Source={dbPath}"))
                    {
                        conn.Open();

                        using (var cmdPragma = conn.CreateCommand())
                        {
                            cmdPragma.CommandText = "PRAGMA foreign_keys = OFF;";
                            cmdPragma.ExecuteNonQuery();
                        }

                        using (var tran = conn.BeginTransaction())
                        {
                            try
                            {
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.Transaction = tran;

                                    // BƯỚC 1: ĐẾM CHÍNH XÁC SỐ DÒNG ĐANG TỒN TẠI TRONG 2 BẢNG
                                    cmd.CommandText = "SELECT COUNT(*) FROM ThongKeCBCS_DuocKhenThuong;";
                                    demThongKe = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

                                    cmd.CommandText = "SELECT COUNT(*) FROM ThongKe_GiayKhen;";
                                    demChiTiet = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

                                    // BƯỚC 2: XÓA SẠCH DỮ LIỆU
                                    cmd.CommandText = "DELETE FROM ThongKeCBCS_DuocKhenThuong;";
                                    cmd.ExecuteNonQuery();

                                    cmd.CommandText = "DELETE FROM ThongKe_GiayKhen;";
                                    cmd.ExecuteNonQuery();

                                    // BƯỚC 3: RESET ID VỀ 1
                                    cmd.CommandText = "DELETE FROM sqlite_sequence WHERE name IN ('ThongKeCBCS_DuocKhenThuong', 'ThongKe_GiayKhen');";
                                    cmd.ExecuteNonQuery();
                                }
                                tran.Commit();
                            }
                            catch
                            {
                                tran.Rollback();
                                throw;
                            }
                        }

                        // DỌN DẸP Ổ CỨNG
                        using (var cmdVacuum = conn.CreateCommand())
                        {
                            cmdVacuum.CommandText = "VACUUM;";
                            cmdVacuum.ExecuteNonQuery();
                        }
                    }

                    // TRẢ VỀ KẾT QUẢ CHO LUỒNG UI
                    return (ThongKe: demThongKe, ChiTiet: demChiTiet);
                });

                // 4. LẤY KẾT QUẢ ĐÃ CHẠY XONG
                int soDongXoa_ThongKe = ketQuaDem.ThongKe;
                int soDongXoa_ChiTiet = ketQuaDem.ChiTiet;

                // Nếu cả 2 đều là 0, nghĩa là bảng đã trống từ trước (Lưới hiển thị là do lệnh LEFT JOIN)
                if (soDongXoa_ThongKe == 0 && soDongXoa_ChiTiet == 0)
                {
                    MessageBox.Show("Các bảng khen thưởng hiện tại đã trống sẵn (0 bản ghi).\nHệ thống đã dọn dẹp và reset lại bộ đếm ID thành công!",
                                    "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Đã xóa sạch toàn bộ dữ liệu khen thưởng thành công!\n" +
                                    $"- Bảng thống kê (Người được khen): {soDongXoa_ThongKe} bản ghi.\n" +
                                    $"- Bảng chi tiết (Số lượng giấy khen): {soDongXoa_ChiTiet} bản ghi.",
                                    "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // 5. GHI NHẬT KÝ
                try
                {
                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM ?? "Admin",
                        "XÓA SẠCH DỮ LIỆU KHEN THƯỞNG",
                        $"Xóa {soDongXoa_ThongKe} dòng Thống kê & {soDongXoa_ChiTiet} dòng Chi tiết lúc {DateTime.Now:HH:mm:ss}");
                }
                catch { }

                // 6. LÀM MỚI GIAO DIỆN
                // Tìm đoạn: 
                // if (dtDanhSachHienThi != null) dtDanhSachHienThi.Clear();
                // THAY THẾ BẰNG:

                if (_danhSachGoc != null) _danhSachGoc.Clear();
                if (_danhSachHienThi != null) _danhSachHienThi.Clear();
                if (kryptonDataGridView1_DanhSachCBCS != null)
                {
                    kryptonDataGridView1_DanhSachCBCS.RowCount = 0;
                    kryptonDataGridView1_DanhSachCBCS.Invalidate();
                }
                kryptonButton_LamMoiCacOTimKiem_Click(null, EventArgs.Empty);
                await ReloadDuLieu();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra lỗi:\n{ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                await ReloadDuLieu();
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }
        private void CapNhatComboBoxTuList()
        {
            if (_danhSachGoc == null) return;
            isUpdatingCombo = true;

            // Nạp Đơn vị
            var dsDonVi = _danhSachGoc.Select(r => r.DonVi).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();
            string luuDonViCu = comboBox_TimKiemDonVi.Text;

            comboBox_TimKiemDonVi.Items.Clear();
            comboBox_TimKiemDonVi.Items.Add("Tất cả");
            foreach (var dv in dsDonVi) comboBox_TimKiemDonVi.Items.Add(dv);

            if (comboBox_TimKiemDonVi.Items.Contains(luuDonViCu)) comboBox_TimKiemDonVi.SelectedItem = luuDonViCu;
            else comboBox_TimKiemDonVi.SelectedIndex = 0;

            // Nạp Khen thưởng
            var dsKhen = _danhSachGoc.Select(r => r.DanhSachDVKhen_An).Where(x => !string.IsNullOrWhiteSpace(x))
                                     .SelectMany(x => x.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                                     .Select(x => x.Trim()).Distinct().OrderBy(x => x).ToList();

            string luuDonViKhenCu = comboBox1_DonViKhenThuong.Text;

            comboBox1_DonViKhenThuong.Items.Clear();
            comboBox1_DonViKhenThuong.Items.Add("Tất cả");
            foreach (var k in dsKhen) comboBox1_DonViKhenThuong.Items.Add(k);

            if (comboBox1_DonViKhenThuong.Items.Contains(luuDonViKhenCu)) comboBox1_DonViKhenThuong.SelectedItem = luuDonViKhenCu;
            else comboBox1_DonViKhenThuong.SelectedIndex = 0;

            isUpdatingCombo = false;
        }
        private void BoLoc_ValueChanged(object sender, EventArgs e)
        {
            if (isUpdatingCombo || _danhSachGoc == null) return;

            try
            {
                string ten = textBox_TimKiemTheoTen?.Text.Trim() ?? "";
                string dvCongTac = comboBox_TimKiemDonVi?.Text ?? "";
                string tinhTrang = comboBox_TinhTrangCongTac?.Text ?? "";
                string dvKhenThuong = comboBox1_DonViKhenThuong?.Text ?? "";

                IEnumerable<ThongTinKhenThuongCBCS> query = _danhSachGoc;

                if (!string.IsNullOrEmpty(ten))
                    query = query.Where(x => (x.HoVaTen ?? "").IndexOf(ten, StringComparison.OrdinalIgnoreCase) >= 0);

                if (!string.IsNullOrEmpty(dvCongTac) && dvCongTac != "Tất cả")
                    query = query.Where(x => string.Equals(x.DonVi, dvCongTac, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(tinhTrang) && tinhTrang != "Tất cả")
                    query = query.Where(x => string.Equals(x.TinhTrang, tinhTrang, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(dvKhenThuong) && dvKhenThuong != "Tất cả")
                    query = query.Where(x => (x.DanhSachDVKhen_An ?? "").IndexOf(dvKhenThuong, StringComparison.OrdinalIgnoreCase) >= 0);

                _danhSachHienThi = query.ToList();

                if (kryptonDataGridView1_DanhSachCBCS != null)
                {
                    kryptonDataGridView1_DanhSachCBCS.RowCount = _danhSachHienThi.Count;
                    kryptonDataGridView1_DanhSachCBCS.Invalidate(); // Ép vẽ lại
                }
                CapNhatThongKeQuanSo();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi lọc: " + ex.Message);
            }
        }
    }
}