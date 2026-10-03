using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PhanMemThiDua2026
{
    public partial class Form31_ChuyenGiaoDuLieu : Form
    {
        private CancellationTokenSource? _ctsTaiThongTinDatabase;
        public Form31_ChuyenGiaoDuLieu(CancellationTokenSource? ctsTaiThongTinDatabase)
        {
            _ctsTaiThongTinDatabase = ctsTaiThongTinDatabase;
        }
        private volatile bool _dangCapNhatCheckedListBox = false;
        private volatile bool _dangXuLyClickCheckedList = false;
        private int _phienTaiDuLieu = 0;
        private volatile bool _dangTaiDuLieu = false;
        private volatile bool _dangXuLyMigration = false;
        private CancellationTokenSource? _boHuyLuong_Quet;
        private string _duongDanCSDL_Nguon = string.Empty;
        private readonly List<string> _danhSachDuongDanThucTe = new(16);
        private readonly Dictionary<string, string> _anhXaTenDatabase = new(StringComparer.OrdinalIgnoreCase) {
            { "csdl1", "Database_1 (Danh tính & Cấu hình)" },
            { "csdl2", "Database_2 (Thông tin chung)" },
            { "csdl3", "Database_3 (Nhật ký hệ thống)" },
            { "csdl4", "Database_4 (Dữ liệu nghiệp vụ Thi đua)" }
        };
        private int _dangTaiDuLieuFlag = 0;
        // ==== WIN32 API — CHỈ DÙNG RIÊNG CHO FORM NÀY ====
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        private const int SW_RESTORE = 9;
        // ==== HẰNG SỐ DÙNG CHUNG (TRÁNH MAGIC STRING) ====
        private const string LUA_CHON_TAT_CA = Module_HeThong.Tat_Ca;
        private static readonly Color MauTieuDe = Color.Red;
        private static readonly Color MauGiaTri = Color.Green;
        // ==== KHÓA/ MỞ VẼ LẠI CONTROL (WM_SETREDRAW) — GIẢM FLICKER & TĂNG HIỆU SUẤT ====
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, bool wParam, IntPtr lParam);
        private const int WM_SETREDRAW = 0x000B;
        public Form31_ChuyenGiaoDuLieu()
        {
            InitializeComponent();
            KhoiTaoFormCauHinh();
            DangKyChuoiSuKien();
            InitToolTips();
        }
        private void Form31_ChuyenGiaoDuLieu_Load(object? sender, EventArgs e)
        {
            richTextBox1_ThongTinDatabaseDuocChon.Clear();
            richTextBox1_ThongTinDatabaseDuocChon.AppendText(
                "Bạn chưa chọn cơ sở dữ liệu...\n" +
                "Vui lòng quét hoặc chọn một cơ sở dữ liệu để xem thông tin."
            );
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Dang_Nhap;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.InitialDelay = 300;
            toolTip1.AutoPopDelay = 2000;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;
            var tips = new Dictionary<Control, string>
            {
                { btn_XuatDuLieuJson, "Xuất dữ liệu hệ thống ra tệp định dạng JSON" },
                { kryptonButton2_MoThuMuc, "Mở thư mục chứa các tệp dữ liệu" },
                { btn_NhapDuLieuJson, "Nhập (Import) dữ liệu từ tệp JSON vào hệ thống" },
                { kryptonButton_Dong, "Đóng cửa sổ làm việc này" },
                { btn_QuetTimKiem, "Quét và tìm kiếm tệp dữ liệu trong thư mục" }
            };
            foreach (var tip in tips)
            {
                if (tip.Key != null) toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }
        private void checkedListBox1_clb_DanhSachBang_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Chặn thực thi nếu hệ thống đang nạp lại danh sách bảng tự động
            if (_dangCapNhatCheckedListBox || _dangXuLyClickCheckedList)
                return;
            try
            {
                _dangXuLyClickCheckedList = true;
                if (sender is not CheckedListBox clb)
                    return;
                int index = clb.SelectedIndex;
                if (index < 0) return; // Không có dòng nào được chọn
                // Tạm hủy lắng nghe sự kiện gốc để chặn vòng lặp kích hoạt liên tục
                clb.ItemCheck -= checkedListBox1_clb_DanhSachBang_ItemCheck;
                // Đảo ngược trạng thái tích chọn hiện tại của dòng
                bool isChecked = clb.GetItemChecked(index);
                clb.SetItemChecked(index, !isChecked);
                // Gọi thủ công hàm xử lý logic gốc để hệ thống ghi nhận trạng thái mới (đồng bộ code gốc)
                var args = new ItemCheckEventArgs(index, !isChecked ? CheckState.Checked : CheckState.Unchecked, isChecked ? CheckState.Checked : CheckState.Unchecked);
                checkedListBox1_clb_DanhSachBang_ItemCheck(clb, args);
                // 🌟 BÍ QUYẾT UX: Xóa vùng bôi xanh dòng ngay lập tức để danh sách nhìn thanh thoát, đẹp mắt
                clb.ClearSelected();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[CheckedListBox UX SelectedIndex Error] " + ex.Message);
            }
            finally
            {
                if (sender is CheckedListBox clb)
                {
                    // Khôi phục liên kết chuỗi sự kiện cho hệ thống
                    clb.ItemCheck -= checkedListBox1_clb_DanhSachBang_ItemCheck;
                    clb.ItemCheck += checkedListBox1_clb_DanhSachBang_ItemCheck;
                }
                _dangXuLyClickCheckedList = false;
            }
        }
        private void checkedListBox1_clb_DanhSachBang_ItemCheck(
            object? sender,
            ItemCheckEventArgs e
        )
        {
            if (_dangCapNhatCheckedListBox)
                return;
            Debug.WriteLine(
                $"[CheckedChanged] Index={e.Index} | {e.NewValue}"
            );
        }
        private void KhoiTaoFormCauHinh()
        {
            SuspendLayout();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            UpdateStyles();
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            prb_TienTrinhChuyenGiao.Visible = false;
            chk_BackupTruocKhiChuyen.Checked = true;
            chk_XoaDuLieuCu.Checked = false;
            CapNhatMauHienThiCheckBox(chk_BackupTruocKhiChuyen);
            CapNhatMauHienThiCheckBox(chk_XoaDuLieuCu);
            lbl_TrangThaiHoatDong.Text = "Sẵn sàng.";
            ResumeLayout(false);
        }
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_COMPOSITED = 0x02000000;
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_COMPOSITED;
                return cp;
            }
        }
        protected override void WndProc(ref Message m)
        {
            const int WM_ERASEBKGND = 0x0014;
            if (m.Msg == WM_ERASEBKGND) return;
            base.WndProc(ref m);
        }
        private void DangKyChuoiSuKien()
        {
            cbo_ChonCSDL_Nguon.SelectedIndexChanged += Cbo_ChonCSDL_Nguon_SelectedIndexChanged;
            btn_QuetTimKiem.Click += btn_QuetTimKiem_Click;
            btn_XuatDuLieuJson.Click += btn_XuatDuLieuJson_Click;
            btn_NhapDuLieuJson.Click += btn_NhapDuLieuJson_Click;
            kryptonButton2_MoThuMuc.Click += kryptonButton2_MoThuMuc_Click;
            chk_BackupTruocKhiChuyen.CheckedChanged += CheckBox_ThayDoiTrangThai;
            chk_XoaDuLieuCu.CheckedChanged += CheckBox_ThayDoiTrangThai;

            // Bổ sung đăng ký sự kiện cho CheckBox Chọn Tất Cả
            checkBox1_ChonTatCaCacCSDL.CheckedChanged += CheckBox1_ChonTatCaCacCSDL_CheckedChanged;

            checkedListBox1_clb_DanhSachBang.SelectedIndexChanged += checkedListBox1_clb_DanhSachBang_SelectedIndexChanged;
            checkedListBox1_clb_DanhSachBang.ItemCheck += checkedListBox1_clb_DanhSachBang_ItemCheck;
        }
        private void CheckBox1_ChonTatCaCacCSDL_CheckedChanged(object? sender, EventArgs e)
        {
            // Ngắt kết nối sự kiện ComboBox để tránh kích hoạt sự kiện chéo (Cascading Events)
            cbo_ChonCSDL_Nguon.SelectedIndexChanged -= Cbo_ChonCSDL_Nguon_SelectedIndexChanged;
            try
            {
                if (checkBox1_ChonTatCaCacCSDL.Checked)
                {
                    if (!cbo_ChonCSDL_Nguon.Items.Contains("Tất cả"))
                    {
                        cbo_ChonCSDL_Nguon.Items.Insert(0, "Tất cả");
                    }
                    cbo_ChonCSDL_Nguon.SelectedIndex = 0;
                }
                else
                {
                    if (cbo_ChonCSDL_Nguon.Items.Contains("Tất cả"))
                    {
                        cbo_ChonCSDL_Nguon.Items.Remove("Tất cả");
                    }

                    if (cbo_ChonCSDL_Nguon.Items.Count > 0)
                    {
                        cbo_ChonCSDL_Nguon.SelectedIndex = 0;
                    }
                }
            }
            finally
            {
                // Khôi phục sự kiện và tự động kích hoạt cập nhật giao diện
                cbo_ChonCSDL_Nguon.SelectedIndexChanged += Cbo_ChonCSDL_Nguon_SelectedIndexChanged;
                Cbo_ChonCSDL_Nguon_SelectedIndexChanged(cbo_ChonCSDL_Nguon, EventArgs.Empty);
            }
        }
        private async Task TaiDuLieuNenAsync()
        {
            if (Interlocked.Exchange(
                ref _dangTaiDuLieuFlag,
                1) == 1)
            {
                return;
            }
            try
            {
                if (IsDisposed || Disposing)
                    return;
                _boHuyLuong_Quet?.Cancel();
                _boHuyLuong_Quet?.Dispose();
                _boHuyLuong_Quet =
                    new CancellationTokenSource();
                CancellationToken token =
                    _boHuyLuong_Quet.Token;
                CapNhatTrangThaiHoatDong(
                    "Đang quét tìm hệ thống dữ liệu...");
                ThietLapTrangThaiTuongTacUI(false);
                List<string> danhSachFile =
                    await Task.Run(() =>
                    {
                        token.ThrowIfCancellationRequested();
                        return Module_ChuyenGiaoDuLieu
                            .NoiVongTayLonKetNoiTimKiemCSDL();
                    }, token);
                token.ThrowIfCancellationRequested();
                if (IsDisposed || Disposing)
                    return;
                await CapNhatComboboxDanhSachAsync(
                    danhSachFile,
                    token);
                if (IsDisposed || Disposing)
                    return;
                // ⭐ BƯỚC CẢI TIẾN UX CHUẨN KỸ SƯ: TỰ ĐỘNG CHỌN DATABASE ĐẦU TIÊN
                // 🛡️ BƯỚC CẢI TIẾN: Chọn Database đầu tiên và ép tải dữ liệu tức thì
                if (cbo_ChonCSDL_Nguon.Items.Count > 0)
                {
                    // Sử dụng BeginInvoke để đảm bảo việc gán Index được thực hiện 
                    // sau khi UI đã sẵn sàng, tránh xung đột render
                    this.BeginInvoke(new Action(() =>
                    {
                        cbo_ChonCSDL_Nguon.SelectedIndex = 0;
                        // 💡 BÍ QUYẾT: Gọi trực tiếp hàm xử lý để UI tự cập nhật RichTextBox ngay
                        // mà không cần chờ người dùng click lại.
                        Cbo_ChonCSDL_Nguon_SelectedIndexChanged(cbo_ChonCSDL_Nguon, EventArgs.Empty);
                    }));
                }
                CapNhatTrangThaiHoatDong(
                    $"Đã xác định {danhSachFile.Count} tệp cơ sở dữ liệu.");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "[TaiDuLieuNenAsync] " + ex);
                if (!IsDisposed && !Disposing)
                {
                    CapNhatTrangThaiHoatDong(
                        "Không thể nạp danh sách dữ liệu.");
                }
            }
            finally
            {
                if (!IsDisposed && !Disposing)
                {
                    ThietLapTrangThaiTuongTacUI(true);
                }
                Interlocked.Exchange(
                    ref _dangTaiDuLieuFlag,
                    0);
            }
        }
        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await Task.Yield();
            _ = TaiDuLieuNenAsync();
        }
        private async Task CapNhatComboboxDanhSachAsync(List<string> danhSach, CancellationToken token)
        {
            await Task.Yield();
            if (IsDisposed) return;

            cbo_ChonCSDL_Nguon.BeginUpdate();
            try
            {
                cbo_ChonCSDL_Nguon.SelectedIndexChanged -= Cbo_ChonCSDL_Nguon_SelectedIndexChanged;
                cbo_ChonCSDL_Nguon.Items.Clear();
                _danhSachDuongDanThucTe.Clear();

                // 🛡️ ĐỒNG BỘ: Nếu CheckBox Chọn tất cả đang active, nạp lại item "Tất cả" ở đầu
                if (checkBox1_ChonTatCaCacCSDL.Checked)
                {
                    cbo_ChonCSDL_Nguon.Items.Add("Tất cả");
                }

                foreach (string duongDan in danhSach)
                {
                    token.ThrowIfCancellationRequested();
                    string tenFile = Path.GetFileNameWithoutExtension(duongDan);
                    string tenHienThi = _anhXaTenDatabase.TryGetValue(tenFile, out string? alias) ? alias : tenFile;
                    cbo_ChonCSDL_Nguon.Items.Add(tenHienThi);
                    _danhSachDuongDanThucTe.Add(duongDan);
                }

                if (cbo_ChonCSDL_Nguon.Items.Count > 0)
                {
                    cbo_ChonCSDL_Nguon.SelectedIndex = 0;
                }
            }
            finally
            {
                cbo_ChonCSDL_Nguon.SelectedIndexChanged += Cbo_ChonCSDL_Nguon_SelectedIndexChanged;
                cbo_ChonCSDL_Nguon.EndUpdate();
            }
        }
        private async void btn_XuatDuLieuJson_Click(object? sender, EventArgs e)
        {
            if (_dangXuLyMigration || _dangTaiDuLieu) return;

            // Trường hợp 1: Chọn tất cả CSDL
            if (checkBox1_ChonTatCaCacCSDL.Checked)
            {
                if (_danhSachDuongDanThucTe.Count == 0)
                {
                    MessageBox.Show("Không tìm thấy cơ sở dữ liệu nào để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _dangXuLyMigration = true;
                ThietLapTrangThaiTuongTacUI(false);
                try
                {
                    string duongDanDesktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string thuMucTong = Path.Combine(duongDanDesktop, "Database-PhanMemThiDua2026");

                    HienThiThanhTienTrinh(_danhSachDuongDanThucTe.Count);
                    int demCSDL = 0;
                    List<string> danhSachLoiTongHop = new();

                    foreach (string duongDanCSDL in _danhSachDuongDanThucTe)
                    {
                        demCSDL++;
                        string tenThuMucRieng = Path.GetFileNameWithoutExtension(duongDanCSDL);
                        string thuMucGoiCon = Path.Combine(thuMucTong, tenThuMucRieng);

                        CapNhatTrangThaiHoatDong($"[{demCSDL}/{_danhSachDuongDanThucTe.Count}] Đang nạp danh sách bảng của: {tenThuMucRieng}...");

                        List<string> danhSachBang = await Task.Run(() => Module_ChuyenGiaoDuLieu.LayDanhSachBang(duongDanCSDL));

                        foreach (string tenBang in danhSachBang)
                        {
                            try
                            {
                                await Task.Run(() => Module_ChuyenGiaoDuLieu.XuatDuLieuRaJson(duongDanCSDL, tenBang, thuMucGoiCon));
                            }
                            catch (Exception exBang)
                            {
                                danhSachLoiTongHop.Add($"- CSDL [{tenThuMucRieng}] - Bảng [{tenBang}]: {exBang.Message}");
                            }
                        }

                        prb_TienTrinhChuyenGiao.Value = demCSDL;
                    }

                    if (danhSachLoiTongHop.Count == 0)
                    {
                        CapNhatTrangThaiHoatDong("100% | Xuất tất cả CSDL thành công!");

                        // ⭐ Ghi nhật ký: Xuất tất cả CSDL thành công
                        Module_NhatKy.GhiNhatKy(
                            taiKhoan: SessionInfo.TenTaiKhoan,
                            hanhDong: "Xuất tất cả CSDL ra JSON",
                            ghiChu: $"Đã xuất thành công toàn bộ {_danhSachDuongDanThucTe.Count} cơ sở dữ liệu | Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                        );

                        MessageBox.Show($"Đã xuất thành công toàn bộ {_danhSachDuongDanThucTe.Count} cơ sở dữ liệu vào thư mục:\nDesktop\\Database-PhanMemThiDua2026", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        CapNhatTrangThaiHoatDong("Hoàn tất với một số cảnh báo.");

                        // ⭐ Ghi nhật ký: Xuất tất cả CSDL có cảnh báo lỗi
                        Module_NhatKy.GhiNhatKy(
                            taiKhoan: SessionInfo.TenTaiKhoan,
                            hanhDong: "Xuất tất cả CSDL ra JSON (Có cảnh báo)",
                            ghiChu: $"Hoàn tất với {danhSachLoiTongHop.Count} lỗi phát sinh | Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                        );

                        string tbLoi = $"Kết xuất hoàn tất.\n\nCác lỗi ghi nhận:\n" + string.Join("\n", danhSachLoiTongHop);
                        MessageBox.Show(tbLoi, "Báo cáo Kết xuất Dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    // ⭐ Ghi nhật ký: Lỗi nghiêm trọng khi xuất tất cả CSDL
                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: SessionInfo.TenTaiKhoan,
                        hanhDong: "Lỗi xuất tất cả CSDL ra JSON",
                        ghiChu: $"Sự cố: {ex.Message} | Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                    );

                    MessageBox.Show($"Sự cố nghiêm trọng khi đóng gói tất cả CSDL:\n{ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    CapNhatTrangThaiHoatDong("Xuất bản lỗi toàn cục.");
                }
                finally
                {
                    _dangXuLyMigration = false;
                    ThietLapTrangThaiTuongTacUI(true);
                    await AnThanhTienTrinhAsync();
                }
                return;
            }

            // Trường hợp 2: Xuất CSDL đơn lẻ hiện tại được chọn (Giữ nguyên logic cũ của bạn)
            if (checkedListBox1_clb_DanhSachBang.CheckedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng tích chọn các bảng cần xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _dangXuLyMigration = true;
            ThietLapTrangThaiTuongTacUI(false);
            try
            {
                string duongDanDesktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string thuMucTong = Path.Combine(duongDanDesktop, "Database-PhanMemThiDua2026");
                string tenThuMucRieng = Path.GetFileNameWithoutExtension(_duongDanCSDL_Nguon);
                string thuMucGoiCon = Path.Combine(thuMucTong, tenThuMucRieng);
                int tongSoBang = checkedListBox1_clb_DanhSachBang.CheckedItems.Count;
                HienThiThanhTienTrinh(tongSoBang);
                int chiSoTienTrinh = 0;
                int soBangThanhCong = 0;
                List<string> danhSachLoi = new();

                foreach (var item in checkedListBox1_clb_DanhSachBang.CheckedItems)
                {
                    string tenBang = item.ToString()!;
                    int phanTram = (int)Math.Round((double)chiSoTienTrinh / tongSoBang * 100);
                    CapNhatTrangThaiHoatDong($"{phanTram}% | Đang kết xuất cấu trúc bảng: {tenBang}...");
                    try
                    {
                        await Task.Run(() => Module_ChuyenGiaoDuLieu.XuatDuLieuRaJson(_duongDanCSDL_Nguon, tenBang, thuMucGoiCon));
                        soBangThanhCong++;
                    }
                    catch (Exception exLoiCucBo)
                    {
                        danhSachLoi.Add($"- Bảng [{tenBang}]: {exLoiCucBo.Message}");
                    }
                    chiSoTienTrinh++;
                    prb_TienTrinhChuyenGiao.Value = chiSoTienTrinh;
                }

                if (danhSachLoi.Count == 0)
                {
                    CapNhatTrangThaiHoatDong("100% | Xuất dữ liệu an toàn thành công!");

                    // ⭐ Ghi nhật ký: Xuất CSDL đơn lẻ thành công
                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: SessionInfo.TenTaiKhoan,
                        hanhDong: "Xuất dữ liệu ra JSON",
                        ghiChu: $"Đã xuất thành công {soBangThanhCong}/{tongSoBang} bảng từ CSDL: {tenThuMucRieng} | Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                    );

                    MessageBox.Show($"Đã xuất thành công {soBangThanhCong}/{tongSoBang} bảng vào thư mục:\nDesktop\\Database-PhanMemThiDua2026\\{tenThuMucRieng}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    CapNhatTrangThaiHoatDong("Hoàn tất với một số cảnh báo.");

                    // ⭐ Ghi nhật ký: Xuất CSDL đơn lẻ có cảnh báo
                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: SessionInfo.TenTaiKhoan,
                        hanhDong: "Xuất dữ liệu ra JSON (Có cảnh báo)",
                        ghiChu: $"Hoàn tất một phần ({soBangThanhCong}/{tongSoBang} bảng) từ CSDL: {tenThuMucRieng} | Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                    );

                    string tbLoi = $"Kết xuất hoàn tất một phần.\nThành công: {soBangThanhCong}/{tongSoBang} bảng.\n\nCác bảng sau bị lỗi (đã được bỏ qua):\n" + string.Join("\n", danhSachLoi);
                    MessageBox.Show(tbLoi, "Báo cáo Kết xuất Dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                // ⭐ Ghi nhật ký: Lỗi nghiêm trọng khi xuất CSDL đơn lẻ
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: SessionInfo.TenTaiKhoan,
                    hanhDong: "Lỗi xuất dữ liệu ra JSON",
                    ghiChu: $"Sự cố: {ex.Message} | Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                );

                MessageBox.Show($"Sự cố nghiêm trọng khi đóng gói dữ liệu:\n{ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                CapNhatTrangThaiHoatDong("Xuất bản lỗi toàn cục.");
            }
            finally
            {
                _dangXuLyMigration = false;
                ThietLapTrangThaiTuongTacUI(true);
                await AnThanhTienTrinhAsync();
            }
        }
        private async void btn_NhapDuLieuJson_Click(object? sender, EventArgs e)
        {
            if (_dangXuLyMigration || _dangTaiDuLieu)
                return;
            string thuMucNguon = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "Database-PhanMemThiDua2026",
                Path.GetFileNameWithoutExtension(_duongDanCSDL_Nguon));
            if (!Directory.Exists(thuMucNguon))
            {
                MessageBox.Show(
                    $"Không tìm thấy gói dữ liệu phân vùng!\n\n{thuMucNguon}",
                    "Thiếu nguồn dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            int tongSoBang = checkedListBox1_clb_DanhSachBang.CheckedItems.Count;
            if (tongSoBang <= 0)
            {
                MessageBox.Show(
                    "Vui lòng chọn bảng dữ liệu cần đồng bộ nạp!",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            if (chk_XoaDuLieuCu.Checked)
            {
                if (MessageBox.Show(
                        "CẢNH BÁO:\n\nToàn bộ dữ liệu cũ sẽ bị xóa trước khi nạp.\nBạn có chắc chắn muốn tiếp tục?",
                        "Xác nhận",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }
            }
            _dangXuLyMigration = true;
            ThietLapTrangThaiTuongTacUI(false);
            try
            {
                string backupFile = string.Empty;
                int soBangThanhCong = 0;
                int chiSoTienTrinh = 0;
                List<string> danhSachLoi = new();
                if (chk_BackupTruocKhiChuyen.Checked)
                {
                    CapNhatTrangThaiHoatDong("Đang tạo điểm khôi phục bảo hiểm...");
                    backupFile = await Task.Run(() =>
                        Module_ChuyenGiaoDuLieu.SaoLuuCSDLTruocKhiNhap(_duongDanCSDL_Nguon));
                }
                HienThiThanhTienTrinh(tongSoBang);
                foreach (object item in checkedListBox1_clb_DanhSachBang.CheckedItems)
                {
                    string tenBang = item.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(tenBang))
                    {
                        chiSoTienTrinh++;
                        continue;
                    }
                    string fileJson = Path.Combine(
                        thuMucNguon,
                        $"{tenBang}.json");
                    int phanTram = (int)Math.Round(
                        (double)chiSoTienTrinh / Math.Max(tongSoBang, 1) * 100);
                    CapNhatTrangThaiHoatDong(
                        $"{phanTram}% | Đang xử lý bảng: {tenBang}");
                    try
                    {
                        if (!File.Exists(fileJson))
                        {
                            throw new FileNotFoundException(
                                $"Không tìm thấy file JSON nguồn: {Path.GetFileName(fileJson)}");
                        }
                        await Task.Run(() =>
                            Module_ChuyenGiaoDuLieu.NhapDuLieuVaoCSDL(
                                fileJson,
                                _duongDanCSDL_Nguon,
                                tenBang,
                                chk_XoaDuLieuCu.Checked,
                                backupFile,
                                thuMucNguon));
                        soBangThanhCong++;
                    }
                    catch (Exception exBang)
                    {
                        danhSachLoi.Add(
                            $"[{tenBang}] : {exBang.Message}");
                    }
                    chiSoTienTrinh++;
                    if (chiSoTienTrinh <= prb_TienTrinhChuyenGiao.Maximum)
                    {
                        prb_TienTrinhChuyenGiao.Value = chiSoTienTrinh;
                    }
                }
                if (soBangThanhCong > 0)
                {
                    CapNhatTrangThaiHoatDong(
                        "Đang tối ưu và dọn dẹp cơ sở dữ liệu...");
                    await Task.Run(() =>
                        Module_ChuyenGiaoDuLieu.VacuumDatabase(
                            _duongDanCSDL_Nguon));
                }
                if (danhSachLoi.Count == 0)
                {
                    CapNhatTrangThaiHoatDong(
                        "Nạp dữ liệu thành công.");

                    // ⭐ BỔ SUNG GHI NHẬT KÝ KHI NẠP DỮ LIỆU THÀNH CÔNG
                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: SessionInfo.TenTaiKhoan,
                        hanhDong: "Nhập dữ liệu JSON",
                        ghiChu: $"Đã nạp thành công {soBangThanhCong}/{tongSoBang} bảng vào CSDL: {Path.GetFileName(_duongDanCSDL_Nguon)} | Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                    );

                    MessageBox.Show(
                        $"Đã nạp thành công {soBangThanhCong}/{tongSoBang} bảng.",
                        "Hoàn tất",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    CapNhatTrangThaiHoatDong(
                        "Hoàn tất với một số lỗi.");

                    // ⭐ BỔ SUNG GHI NHẬT KÝ KHI NẠP DỮ LIỆU CÓ CẢNH BÁO/LỖI
                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: SessionInfo.TenTaiKhoan,
                        hanhDong: "Nhập dữ liệu JSON (Có lỗi)",
                        ghiChu: $"Nạp hoàn tất một phần ({soBangThanhCong}/{tongSoBang} bảng) vào CSDL: {Path.GetFileName(_duongDanCSDL_Nguon)} | Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                    );

                    MessageBox.Show(
                        $"Đã nạp thành công {soBangThanhCong}/{tongSoBang} bảng.\n\n" +
                        string.Join(Environment.NewLine, danhSachLoi),
                        "Báo cáo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                await TaiDuLieuNenAsync();
            }
            catch (Exception ex)
            {
                CapNhatTrangThaiHoatDong( "Tiến trình bị hủy do lỗi.");
                // ⭐ BỔ SUNG GHI NHẬT KÝ KHI XẢY RA SỰ CỐ NGHIÊM TRỌNG
                Module_NhatKy.GhiNhatKy(
                    taiKhoan: SessionInfo.TenTaiKhoan,
                    hanhDong: "Lỗi Nhập dữ liệu JSON",
                    ghiChu: $"Sự cố: {ex.Message} | Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss}"
                );
                MessageBox.Show(
                    ex.Message,
                    "Lỗi Di Trú",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _dangXuLyMigration = false;
                ThietLapTrangThaiTuongTacUI(true);
                await AnThanhTienTrinhAsync();
            }
        }
        /// <summary>
        /// Hiển thị khối thông tin tóm tắt khi người dùng chọn gộp "Tất cả" CSDL.
        /// Dùng WM_SETREDRAW để khóa vẽ lại trong lúc ghi nhiều dòng màu,
        /// tránh flicker và giảm số lần layout/repaint của RichTextBox.
        /// </summary>
        private void HienThiThongTinChonTatCa()
        {
            var rtb = richTextBox1_ThongTinDatabaseDuocChon;

            rtb.Clear();
            SuspendDrawing(rtb);
            try
            {
                AppendDongThongTin(rtb, "Tên tài khoản: ", $"{Module_TaiKhoan.TenTaiKhoan_RAM}\n");
                AppendDongThongTin(rtb, "Hành động: ", "Đang chọn tất cả cơ sở dữ liệu...\n");
                AppendDongThongTin(rtb, "Tổng số tệp CSDL: ", $"{_danhSachDuongDanThucTe.Count}\n");

                // Dòng thông báo đơn (không có tiêu đề) -> chữ xanh lá
                rtb.SelectionColor = MauGiaTri;
                rtb.AppendText("Khi xuất dữ liệu, hệ thống sẽ tự động duyệt qua và xuất toàn bộ các CSDL trong danh sách.\n");

                // Trả lại màu chữ mặc định cho phần nhập tiếp theo (nếu có)
                rtb.SelectionColor = rtb.ForeColor;
            }
            finally
            {
                ResumeDrawing(rtb);
            }
        }
        /// <summary>
        /// Ghi một dòng "Tiêu đề: Giá trị" vào RichTextBox với 2 màu riêng biệt
        /// (tiêu đề màu đỏ, giá trị màu xanh lá) — dùng chung cho mọi màn hình hiển thị thông tin CSDL.
        /// </summary>
        private static void AppendDongThongTin(RichTextBox rtb, string tieuDe, string giaTri)
        {
            rtb.SelectionColor = MauTieuDe;
            rtb.AppendText(tieuDe);
            rtb.SelectionColor = MauGiaTri;
            rtb.AppendText(giaTri);
        }
        /// <summary>
        /// Thực hiện thao tác cập nhật <see cref="checkedListBox1_clb_DanhSachBang"/> một cách an toàn:
        /// tạm ngắt sự kiện SelectedIndexChanged + khóa vẽ lại (BeginUpdate/EndUpdate),
        /// tránh vòng lặp sự kiện (event loop) và giảm nháy hình khi thao tác hàng loạt item.
        /// </summary>
        private void LamMoiCheckedListBoxAnToan(Action thaoTac)
        {
            var clb = checkedListBox1_clb_DanhSachBang;

            _dangCapNhatCheckedListBox = true;
            clb.SelectedIndexChanged -= checkedListBox1_clb_DanhSachBang_SelectedIndexChanged;
            clb.BeginUpdate();
            try
            {
                thaoTac();
            }
            finally
            {
                clb.EndUpdate();
                clb.SelectedIndexChanged += checkedListBox1_clb_DanhSachBang_SelectedIndexChanged;
                _dangCapNhatCheckedListBox = false;
            }
        }
        private static void SuspendDrawing(Control control) =>
            SendMessage(control.Handle, WM_SETREDRAW, false, IntPtr.Zero);
        private static void ResumeDrawing(Control control)
        {
            SendMessage(control.Handle, WM_SETREDRAW, true, IntPtr.Zero);
            control.Invalidate(); // Vẽ lại một lần duy nhất sau khi hoàn tất toàn bộ thao tác
        }
        // ==== HẰNG SỐ DÙNG CHUNG (ĐẶT Ở ĐẦU CLASS, NGOÀI HÀM NÀY) ====
        // private const string LUA_CHON_TAT_CA = "Tất cả";
        // private static readonly Color MauTieuDe = Color.Red;
        // private static readonly Color MauGiaTri = Color.Green;
        private async void Cbo_ChonCSDL_Nguon_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_dangXuLyMigration)
                return;

            int index = cbo_ChonCSDL_Nguon.SelectedIndex;
            if (index < 0)
                return;

            string itemChon = cbo_ChonCSDL_Nguon.SelectedItem?.ToString() ?? "";

            // 🔄 TỰ ĐỘNG ĐỒNG BỘ NGUỢC LẠI CHECKBOX
            checkBox1_ChonTatCaCacCSDL.CheckedChanged -= CheckBox1_ChonTatCaCacCSDL_CheckedChanged;
            try
            {
                checkBox1_ChonTatCaCacCSDL.Checked = (itemChon == LUA_CHON_TAT_CA);
            }
            finally
            {
                checkBox1_ChonTatCaCacCSDL.CheckedChanged += CheckBox1_ChonTatCaCacCSDL_CheckedChanged;
            }

            // 1. Nếu item được chọn là "Tất cả"
            if (itemChon == LUA_CHON_TAT_CA)
            {
                HienThiThongTinChonTatCa();
                LamMoiCheckedListBoxAnToan(() => checkedListBox1_clb_DanhSachBang.Items.Clear());
                CapNhatTrangThaiHoatDong("Đã chọn tất cả cơ sở dữ liệu.");
                return;
            }

            // 2. Nếu chọn cơ sở dữ liệu đơn lẻ: Tính toán index thực tế trong _danhSachDuongDanThucTe
            bool coItemTatCa = cbo_ChonCSDL_Nguon.Items.Contains(LUA_CHON_TAT_CA);
            int indexThucTe = coItemTatCa ? index - 1 : index;

            if (indexThucTe < 0 || indexThucTe >= _danhSachDuongDanThucTe.Count)
                return;

            string duongDanFile = _danhSachDuongDanThucTe[indexThucTe];
            _duongDanCSDL_Nguon = duongDanFile;
            int version = Interlocked.Increment(ref _phienTaiDuLieu);

            try
            {
                ThietLapTrangThaiTuongTacUI(false);
                CapNhatTrangThaiHoatDong("Đang phân tích cơ sở dữ liệu...");
                richTextBox1_ThongTinDatabaseDuocChon.Clear();

                List<string> danhSachBang = await Task.Run(() => Module_ChuyenGiaoDuLieu.LayDanhSachBang(duongDanFile));

                if (version != _phienTaiDuLieu || IsDisposed || Disposing)
                    return;

                LamMoiCheckedListBoxAnToan(() =>
                {
                    checkedListBox1_clb_DanhSachBang.Items.Clear();
                    foreach (string tenBang in danhSachBang)
                    {
                        int idx = checkedListBox1_clb_DanhSachBang.Items.Add(tenBang);
                        checkedListBox1_clb_DanhSachBang.SetItemChecked(idx, true);
                    }
                });

                Module_ChuyenGiaoDuLieu.InThongTinDatabaseLenRichTextBox(
                    richTextBox1_ThongTinDatabaseDuocChon,
                    duongDanFile,
                    danhSachBang);
                CapNhatTrangThaiHoatDong("Đã nạp thông tin phân vùng dữ liệu.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Database Load] " + ex);
                if (!IsDisposed && !Disposing)
                {
                    Module_ChuyenGiaoDuLieu.InThongTinDatabaseLenRichTextBox(
                        richTextBox1_ThongTinDatabaseDuocChon,
                        duongDanFile,
                        ex.Message);
                    CapNhatTrangThaiHoatDong("Không thể phân tích dữ liệu.");
                }
            }
            finally
            {
                if (!IsDisposed && !Disposing)
                {
                    ThietLapTrangThaiTuongTacUI(true);
                }
            }
        }
        private async void kryptonButton2_MoThuMuc_Click(object? sender, EventArgs e)
        {
            if (_dangTaiDuLieu || _dangXuLyMigration) return;

            try
            {
                kryptonButton2_MoThuMuc.Enabled = false;
                CapNhatTrangThaiHoatDong("Đang kiểm tra cửa sổ thư mục...");
                await Task.Yield(); // Nhường UI cập nhật trạng thái trước khi xử lý

                string thuMucTong = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "Database-PhanMemThiDua2026");

                if (!Directory.Exists(thuMucTong))
                {
                    Directory.CreateDirectory(thuMucTong);
                }

                // ⭐ BƯỚC 1: DÒ XEM ĐÃ CÓ CỬA SỔ EXPLORER NÀO MỞ SẴN THƯ MỤC NÀY CHƯA
                // (Chạy trên UI thread vì Shell.Application là COM STA, tránh lỗi marshal xuyên luồng)
                bool daKichHoatCuaSoCu = KichHoatCuaSoDaMoNeuCo(thuMucTong);

                if (daKichHoatCuaSoCu)
                {
                    CapNhatTrangThaiHoatDong("Đã có cửa sổ thư mục đang mở — đưa lên trên.");
                }
                else
                {
                    // ⭐ BƯỚC 2: KHÔNG TÌM THẤY -> MỞ CỬA SỔ MỚI (chạy nền để không chặn UI)
                    CapNhatTrangThaiHoatDong("Đang mở thư mục...");
                    await Task.Run(() => MoThuMucMoiTrongExplorer(thuMucTong));
                    CapNhatTrangThaiHoatDong("Sẵn sàng.");
                }
            }
            finally
            {
                kryptonButton2_MoThuMuc.Enabled = true;
            }
        }
        /// <summary>
        /// Dò trong danh sách cửa sổ Explorer đang mở (qua COM Shell.Application).
        /// Nếu tìm thấy cửa sổ đang trỏ ĐÚNG thư mục cần mở, đưa cửa sổ đó lên foreground
        /// (khôi phục nếu đang thu nhỏ) thay vì mở thêm cửa sổ mới.
        /// </summary>
        /// <returns>true nếu tìm thấy và đã kích hoạt thành công; false nếu chưa có cửa sổ nào.</returns>
        private bool KichHoatCuaSoDaMoNeuCo(string duongDanThuMuc)
        {
            dynamic? shellApp = null;
            dynamic? danhSachCuaSo = null;

            try
            {
                Type? shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null) return false;

                shellApp = Activator.CreateInstance(shellType);
                if (shellApp == null) return false;

                danhSachCuaSo = shellApp.Windows();
                string duongDanCanTim = ChuanHoaDuongDan(duongDanThuMuc);

                foreach (dynamic cuaSo in danhSachCuaSo)
                {
                    try
                    {
                        string? locationUrl = cuaSo.LocationURL as string;
                        if (string.IsNullOrEmpty(locationUrl)) continue;

                        string duongDanCuaSoNay = ChuanHoaDuongDan(
                            Uri.UnescapeDataString(new Uri(locationUrl).LocalPath));

                        if (!string.Equals(duongDanCuaSoNay, duongDanCanTim, StringComparison.OrdinalIgnoreCase))
                            continue;

                        IntPtr hwnd = new IntPtr((long)cuaSo.HWND);
                        if (hwnd == IntPtr.Zero) continue;

                        if (IsIconic(hwnd))
                            ShowWindowAsync(hwnd, SW_RESTORE);

                        SetForegroundWindow(hwnd);
                        return true;
                    }
                    catch
                    {
                        // Cửa sổ này không phải Explorer hợp lệ (VD: IE, hoặc vừa bị đóng) -> bỏ qua
                    }
                    finally
                    {
                        if (cuaSo != null) Marshal.ReleaseComObject(cuaSo);
                    }
                }
            }
            catch (Exception ex)
            {
                // COM có thể bị chặn bởi Group Policy hoặc không khả dụng -> âm thầm fallback mở cửa sổ mới
                Debug.WriteLine("[Shell COM Error]: " + ex.Message);
            }
            finally
            {
                if (danhSachCuaSo != null) Marshal.ReleaseComObject(danhSachCuaSo);
                if (shellApp != null) Marshal.ReleaseComObject(shellApp);
            }

            return false;
        }
        /// <summary>Chuẩn hóa đường dẫn để so sánh chính xác (bỏ dấu "/" cuối, quy về full path).</summary>
        private static string ChuanHoaDuongDan(string duongDan) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(duongDan));
        /// <summary>
        /// Mở một cửa sổ Explorer MỚI trỏ đúng vào thư mục tổng.
        /// Chỉ gọi khi đã xác nhận KHÔNG có cửa sổ nào đang mở sẵn thư mục này.
        /// </summary>
        private static void MoThuMucMoiTrongExplorer(string duongDanThuMuc)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{duongDanThuMuc}\"",
                    UseShellExecute = true,
                    ErrorDialog = false
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Explorer Error]: " + ex.Message);
            }
        }
        private void btn_QuetTimKiem_Click(object? sender, EventArgs e)
            {
                _ = TaiDuLieuNenAsync();
            }
        private void CapNhatTrangThaiHoatDong(string text)
        {
            if (IsDisposed) return;
            lbl_TrangThaiHoatDong.Text = text;
        }
        // PROGRESS ENGINE
        private void HienThiThanhTienTrinh(
            int giaTriToiDa
        )
        {
            if (IsDisposed)
                return;
            if (!IsHandleCreated)
                return;
            try
            {
                prb_TienTrinhChuyenGiao.Visible = true;
                prb_TienTrinhChuyenGiao.Minimum = 0;
                prb_TienTrinhChuyenGiao.Maximum =
                    Math.Max(1, giaTriToiDa);
                prb_TienTrinhChuyenGiao.Value = 0;
                prb_TienTrinhChuyenGiao.Style =
                    ProgressBarStyle.Continuous;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "[HienThiThanhTienTrinh] " + ex.Message
                );
            }
        }
        private async Task AnThanhTienTrinhAsync()
        {
            try
            {
                await Task.Delay(1000);
                if (IsDisposed)
                    return;
                if (!IsHandleCreated)
                    return;
                if (_dangXuLyMigration)
                    return;
                BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (IsDisposed)
                            return;
                        prb_TienTrinhChuyenGiao.Value = 0;
                        prb_TienTrinhChuyenGiao.Visible = false;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(
                            "[AnThanhTienTrinhAsync] "
                            + ex.Message
                        );
                    }
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "[AnThanhTienTrinhAsync] "
                    + ex.Message
                );
            }
        }
        private void ThietLapTrangThaiTuongTacUI(bool trangThai)
        {
            btn_QuetTimKiem.Enabled = trangThai;
            btn_XuatDuLieuJson.Enabled = trangThai;
            btn_NhapDuLieuJson.Enabled = trangThai;
            kryptonButton2_MoThuMuc.Enabled = trangThai;
            cbo_ChonCSDL_Nguon.Enabled = trangThai;
            checkedListBox1_clb_DanhSachBang.Enabled = trangThai;
            chk_BackupTruocKhiChuyen.Enabled = trangThai;
            chk_XoaDuLieuCu.Enabled = trangThai;
        }
        private void CheckBox_ThayDoiTrangThai(object? sender, EventArgs e)
        {
            if (sender is CheckBox chk) CapNhatMauHienThiCheckBox(chk);
        }
        private void CapNhatMauHienThiCheckBox(CheckBox chk)
        {
            if (chk.Checked)
            {
                chk.BackColor = Color.FromArgb(230, 245, 233);
                chk.ForeColor = Color.DarkGreen;
            }
            else
            {
                chk.BackColor = Color.FromArgb(253, 238, 238);
                chk.ForeColor = Color.DarkRed;
            }
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_dangXuLyMigration)
            {
                e.Cancel = true;
                MessageBox.Show("Hệ thống đang thực thi ghi cơ sở dữ liệu ngầm an toàn. Vui lòng không đóng phần mềm lúc này để tránh hỏng cấu trúc tệp tin!", "Cảnh báo an toàn dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            _boHuyLuong_Quet?.Cancel();
            _boHuyLuong_Quet?.Dispose();
            base.OnFormClosing(e);
        }
        private void kryptonButton_Dong_Click(
                 object? sender,
                 EventArgs e
             )
        {
            try
            {
                // CHỐNG ĐÓNG KHI ĐANG MIGRATION
                if (_dangXuLyMigration)
                {
                    MessageBox.Show(
                        "Hệ thống đang xử lý dữ liệu nền an toàn.\nVui lòng chờ hoàn tất trước khi đóng.",
                        "Đang xử lý dữ liệu",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }
                // YÊU CẦU HỦY TASK NỀN
                // KHÔNG DISPOSE TẠI ĐÂY
                try
                {
                    if (_boHuyLuong_Quet != null)
                    {
                        if (!_boHuyLuong_Quet.IsCancellationRequested)
                        {
                            _boHuyLuong_Quet.Cancel();
                        }
                    }
                    if (_ctsTaiThongTinDatabase != null)
                    {
                        if (!_ctsTaiThongTinDatabase.IsCancellationRequested)
                        {
                            _ctsTaiThongTinDatabase.Cancel();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "[Cancel CTS] " + ex.Message
                    );
                }
                // KHÔI PHỤC FORM12
                Form12? form12 =
                    Application.OpenForms["Form12"] as Form12;
                if (form12 != null)
                {
                    if (!form12.Visible)
                    {
                        form12.Show();
                    }
                    if (form12.WindowState == FormWindowState.Minimized)
                    {
                        form12.WindowState =
                            FormWindowState.Normal;
                    }
                    form12.BringToFront();
                    form12.Activate();
                }
                // ĐÓNG FORM
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "[kryptonButton_Dong_Click] "
                    + ex
                );
                try
                {
                    Close();
                }
                catch
                {
                }
            }
        }
    }
}
