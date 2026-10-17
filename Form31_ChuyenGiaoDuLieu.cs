
///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PhanMemThiDua2026
{
    public partial class Form31_ChuyenGiaoDuLieu : Form
    {
        private CancellationTokenSource? _ctsTaiThongTinDatabase;
        // Khai báo biến Panel tiến trình tùy chỉnh trong class Form31_ChuyenGiaoDuLieu
        private Panel? _customProgressBar;
        private System.Windows.Forms.Timer? _progressAnimTimer;
        private float _progressPercent = 0f;
        private float _animOffset = 0f;
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

        // ==== HẰNG SỐ DÙNG CHUNG ====
        private const string LUA_CHON_TAT_CA = Module_HeThong.Tat_Ca;
        private static readonly Color MauTieuDe = Color.Red;
        private static readonly Color MauGiaTri = Color.Green;

        [DllImport("user32.dll")]
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
                { btn_QuetTimKiem, "Quét và tìm kiếm tệp dữ liệu trong thư mục Desktop" }
            };
            foreach (var tip in tips)
            {
                if (tip.Key != null) toolTip1.SetToolTip(tip.Key, tip.Value);
            }
        }

        private void checkedListBox1_clb_DanhSachBang_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_dangCapNhatCheckedListBox || _dangXuLyClickCheckedList)
                return;
            try
            {
                _dangXuLyClickCheckedList = true;
                if (sender is not CheckedListBox clb)
                    return;
                int index = clb.SelectedIndex;
                if (index < 0) return;

                clb.ItemCheck -= checkedListBox1_clb_DanhSachBang_ItemCheck;
                bool isChecked = clb.GetItemChecked(index);
                clb.SetItemChecked(index, !isChecked);

                var args = new ItemCheckEventArgs(index, !isChecked ? CheckState.Checked : CheckState.Unchecked, isChecked ? CheckState.Checked : CheckState.Unchecked);
                checkedListBox1_clb_DanhSachBang_ItemCheck(clb, args);
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
                    clb.ItemCheck -= checkedListBox1_clb_DanhSachBang_ItemCheck;
                    clb.ItemCheck += checkedListBox1_clb_DanhSachBang_ItemCheck;
                }
                _dangXuLyClickCheckedList = false;
            }
        }

        private void checkedListBox1_clb_DanhSachBang_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            if (_dangCapNhatCheckedListBox)
                return;
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
            checkBox1_ChonTatCaCacCSDL.CheckedChanged += CheckBox1_ChonTatCaCacCSDL_CheckedChanged;
            checkedListBox1_clb_DanhSachBang.SelectedIndexChanged += checkedListBox1_clb_DanhSachBang_SelectedIndexChanged;
            checkedListBox1_clb_DanhSachBang.ItemCheck += checkedListBox1_clb_DanhSachBang_ItemCheck;
        }

        private void CheckBox1_ChonTatCaCacCSDL_CheckedChanged(object? sender, EventArgs e)
        {
            cbo_ChonCSDL_Nguon.SelectedIndexChanged -= Cbo_ChonCSDL_Nguon_SelectedIndexChanged;
            try
            {
                if (checkBox1_ChonTatCaCacCSDL.Checked)
                {
                    if (!cbo_ChonCSDL_Nguon.Items.Contains(LUA_CHON_TAT_CA))
                    {
                        cbo_ChonCSDL_Nguon.Items.Insert(0, LUA_CHON_TAT_CA);
                    }
                    cbo_ChonCSDL_Nguon.SelectedIndex = 0;
                }
                else
                {
                    if (cbo_ChonCSDL_Nguon.Items.Contains(LUA_CHON_TAT_CA))
                    {
                        cbo_ChonCSDL_Nguon.Items.Remove(LUA_CHON_TAT_CA);
                    }
                    if (cbo_ChonCSDL_Nguon.Items.Count > 0)
                    {
                        cbo_ChonCSDL_Nguon.SelectedIndex = 0;
                    }
                }
            }
            finally
            {
                cbo_ChonCSDL_Nguon.SelectedIndexChanged += Cbo_ChonCSDL_Nguon_SelectedIndexChanged;
                Cbo_ChonCSDL_Nguon_SelectedIndexChanged(cbo_ChonCSDL_Nguon, EventArgs.Empty);
            }
        }

        /// <summary>
        /// ⭐ SỬA ĐỔI THÔNG MINH: Quét kiểm tra chính xác các CSDL đang thực tế tồn tại trên Desktop
        /// </summary>
        private async Task TaiDuLieuNenAsync()
        {
            if (Interlocked.Exchange(ref _dangTaiDuLieuFlag, 1) == 1)
                return;

            try
            {
                if (IsDisposed || Disposing) return;

                _boHuyLuong_Quet?.Cancel();
                _boHuyLuong_Quet?.Dispose();
                _boHuyLuong_Quet = new CancellationTokenSource();
                CancellationToken token = _boHuyLuong_Quet.Token;

                CapNhatTrangThaiHoatDong("Đang quét kiểm tra dữ liệu trên Desktop...");
                ThietLapTrangThaiTuongTacUI(false);

                // Lấy danh sách CSDL thực tế tồn tại
                List<string> danhSachFile = await Task.Run(() => LayDanhSachCSDLThucTeTonTai(), token);

                token.ThrowIfCancellationRequested();
                if (IsDisposed || Disposing) return;

                await CapNhatComboboxDanhSachAsync(danhSachFile, token);

                if (cbo_ChonCSDL_Nguon.Items.Count > 0)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        cbo_ChonCSDL_Nguon.SelectedIndex = 0;
                        Cbo_ChonCSDL_Nguon_SelectedIndexChanged(cbo_ChonCSDL_Nguon, EventArgs.Empty);
                    }));
                }
                else
                {
                    richTextBox1_ThongTinDatabaseDuocChon.Clear();
                    richTextBox1_ThongTinDatabaseDuocChon.AppendText("Không tìm thấy cơ sở dữ liệu nào trong thư mục Database-PhanMemThiDua2026 trên Desktop.");
                    LamMoiCheckedListBoxAnToan(() => checkedListBox1_clb_DanhSachBang.Items.Clear());
                }

                CapNhatTrangThaiHoatDong($"Đã xác định {danhSachFile.Count} tệp/phân vùng dữ liệu thực tế.");
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine("[TaiDuLieuNenAsync] " + ex);
                if (!IsDisposed && !Disposing)
                {
                    CapNhatTrangThaiHoatDong("Không thể nạp danh sách dữ liệu.");
                }
            }
            finally
            {
                if (!IsDisposed && !Disposing)
                {
                    ThietLapTrangThaiTuongTacUI(true);
                }
                Interlocked.Exchange(ref _dangTaiDuLieuFlag, 0);
            }
        }

        /// <summary>
        /// Hàm phụ trợ: Quét thư mục Desktop xem có csdl1..csdl4 dạng file (.db/.sqlite) hoặc thư mục chứa JSON hay không
        /// </summary>
        private List<string> LayDanhSachCSDLThucTeTonTai()
        {
            List<string> ketQua = new();
            string desktopStandard = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string desktopUserProfile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop");
            string desktopOneDrive = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive", "Desktop");

            string folderDesktop = "";
            string tenThuMuc = "Database-PhanMemThiDua2026";

            if (Directory.Exists(Path.Combine(desktopStandard, tenThuMuc))) folderDesktop = Path.Combine(desktopStandard, tenThuMuc);
            else if (Directory.Exists(Path.Combine(desktopUserProfile, tenThuMuc))) folderDesktop = Path.Combine(desktopUserProfile, tenThuMuc);
            else if (Directory.Exists(Path.Combine(desktopOneDrive, tenThuMuc))) folderDesktop = Path.Combine(desktopOneDrive, tenThuMuc);

            // Ưu tiên 1: Lấy CSDL gốc từ hệ thống nếu không tìm thấy thư mục trên Desktop
            List<string> csdlGoc = Module_ChuyenGiaoDuLieu.NoiVongTayLonKetNoiTimKiemCSDL();

            if (string.IsNullOrEmpty(folderDesktop) || !Directory.Exists(folderDesktop))
            {
                return csdlGoc;
            }

            // Ưu tiên 2: Quét thư mục trên Desktop
            string[] dbTargets = { "csdl1", "csdl2", "csdl3", "csdl4" };
            foreach (string dbName in dbTargets)
            {
                // Kiểm tra xem có thư mục con (ví dụ Desktop\Database-PhanMemThiDua2026\csdl1) chứa file json không
                string subFolder = Path.Combine(folderDesktop, dbName);
                bool coThuMucJson = Directory.Exists(subFolder) && Directory.GetFiles(subFolder, "*.json").Length > 0;

                // Kiểm tra xem có file db trực tiếp không
                string[] files = Directory.GetFiles(folderDesktop, $"{dbName}.*", SearchOption.TopDirectoryOnly);

                if (coThuMucJson || files.Length > 0)
                {
                    // Lấy đường dẫn CSDL gốc tương ứng để sẵn sàng nạp
                    string? matchGoc = csdlGoc.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(dbName, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(matchGoc))
                    {
                        ketQua.Add(matchGoc);
                    }
                }
            }

            // Nếu trên Desktop rỗng hoàn toàn, trả về danh sách CSDL gốc hệ thống
            return ketQua.Count > 0 ? ketQua : csdlGoc;
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

                if (checkBox1_ChonTatCaCacCSDL.Checked)
                {
                    cbo_ChonCSDL_Nguon.Items.Add(LUA_CHON_TAT_CA);
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

        /// <summary>
        /// ⭐ SỬA ĐỔI THÔNG MINH: Bổ sung tính năng NẠP TẤT CẢ CSDL khi tích chọn "Chọn tất cả"
        /// </summary>
        private async void btn_NhapDuLieuJson_Click(object? sender, EventArgs e)
        {
            if (_dangXuLyMigration || _dangTaiDuLieu)
                return;

            string thuMucTongNguon = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "Database-PhanMemThiDua2026");

            if (!Directory.Exists(thuMucTongNguon))
            {
                MessageBox.Show($"Không tìm thấy thư mục gói dữ liệu trên Desktop!\n\n{thuMucTongNguon}", "Thiếu nguồn dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (chk_XoaDuLieuCu.Checked)
            {
                if (MessageBox.Show("CẢNH BÁO:\n\nToàn bộ dữ liệu cũ sẽ bị xóa trước khi nạp.\nBạn có chắc chắn muốn tiếp tục?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }
            }

            _dangXuLyMigration = true;
            ThietLapTrangThaiTuongTacUI(false);

            try
            {
                // ==================== TRƯỜNG HỢP 1: NẠP TẤT CẢ CSDL ====================
                if (checkBox1_ChonTatCaCacCSDL.Checked)
                {
                    if (_danhSachDuongDanThucTe.Count == 0)
                    {
                        MessageBox.Show("Không tìm thấy CSDL đích nào để nạp!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    int demCSDL = 0;
                    int tongThanhCong = 0;
                    List<string> danhSachLoiTongHop = new();

                    HienThiThanhTienTrinh(_danhSachDuongDanThucTe.Count);

                    foreach (string csdlPath in _danhSachDuongDanThucTe)
                    {
                        demCSDL++;
                        string dbName = Path.GetFileNameWithoutExtension(csdlPath);
                        string folderCon = Path.Combine(thuMucTongNguon, dbName);

                        if (!Directory.Exists(folderCon)) continue;

                        string[] jsonFiles = Directory.GetFiles(folderCon, "*.json");
                        if (jsonFiles.Length == 0) continue;

                        CapNhatTrangThaiHoatDong($"[{demCSDL}/{_danhSachDuongDanThucTe.Count}] Đang nạp CSDL: {dbName}...");

                        string backupFile = "";
                        if (chk_BackupTruocKhiChuyen.Checked)
                        {
                            backupFile = await Task.Run(() => Module_ChuyenGiaoDuLieu.SaoLuuCSDLTruocKhiNhap(csdlPath));
                        }

                        foreach (string fileJson in jsonFiles)
                        {
                            string tenBang = Path.GetFileNameWithoutExtension(fileJson);
                            try
                            {
                                await Task.Run(() => Module_ChuyenGiaoDuLieu.NhapDuLieuVaoCSDL(fileJson, csdlPath, tenBang, chk_XoaDuLieuCu.Checked, backupFile, folderCon));
                                tongThanhCong++;
                            }
                            catch (Exception ex)
                            {
                                danhSachLoiTongHop.Add($"[{dbName} -> {tenBang}]: {ex.Message}");
                            }
                        }

                        await Task.Run(() => Module_ChuyenGiaoDuLieu.VacuumDatabase(csdlPath));
                        prb_TienTrinhChuyenGiao.Value = demCSDL;
                    }

                    if (danhSachLoiTongHop.Count == 0)
                    {
                        CapNhatTrangThaiHoatDong("Nạp tất cả CSDL thành công!");
                        Module_NhatKy.GhiNhatKy(SessionInfo.TenTaiKhoan, "Nhập tất cả CSDL từ JSON", $"Thành công nạp tất cả CSDL | {DateTime.Now}");
                        MessageBox.Show("Đã nạp thành công toàn bộ cơ sở dữ liệu từ thư mục Desktop!", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        CapNhatTrangThaiHoatDong("Hoàn tất với một số lỗi.");
                        MessageBox.Show("Đã nạp xong tất cả CSDL nhưng có một số bảng bị lỗi:\n\n" + string.Join("\n", danhSachLoiTongHop), "Báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    await TaiDuLieuNenAsync();
                    return;
                }

                // ==================== TRƯỜNG HỢP 2: NẠP CSDL ĐƠN LẺ ====================
                string thuMucNguon = Path.Combine(thuMucTongNguon, Path.GetFileNameWithoutExtension(_duongDanCSDL_Nguon));
                if (!Directory.Exists(thuMucNguon))
                {
                    MessageBox.Show($"Không tìm thấy gói dữ liệu phân vùng!\n\n{thuMucNguon}", "Thiếu nguồn dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int tongSoBang = checkedListBox1_clb_DanhSachBang.CheckedItems.Count;
                if (tongSoBang <= 0)
                {
                    MessageBox.Show("Vui lòng chọn bảng dữ liệu cần đồng bộ nạp!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string backupFileDonLe = string.Empty;
                int soBangThanhCong = 0;
                int chiSoTienTrinh = 0;
                List<string> danhSachLoi = new();

                if (chk_BackupTruocKhiChuyen.Checked)
                {
                    CapNhatTrangThaiHoatDong("Đang tạo điểm khôi phục bảo hiểm...");
                    backupFileDonLe = await Task.Run(() => Module_ChuyenGiaoDuLieu.SaoLuuCSDLTruocKhiNhap(_duongDanCSDL_Nguon));
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
                    string fileJson = Path.Combine(thuMucNguon, $"{tenBang}.json");
                    int phanTram = (int)Math.Round((double)chiSoTienTrinh / Math.Max(tongSoBang, 1) * 100);
                    CapNhatTrangThaiHoatDong($"{phanTram}% | Đang xử lý bảng: {tenBang}");
                    try
                    {
                        if (!File.Exists(fileJson))
                        {
                            throw new FileNotFoundException($"Không tìm thấy file JSON nguồn: {Path.GetFileName(fileJson)}");
                        }
                        await Task.Run(() => Module_ChuyenGiaoDuLieu.NhapDuLieuVaoCSDL(fileJson, _duongDanCSDL_Nguon, tenBang, chk_XoaDuLieuCu.Checked, backupFileDonLe, thuMucNguon));
                        soBangThanhCong++;
                    }
                    catch (Exception exBang)
                    {
                        danhSachLoi.Add($"[{tenBang}] : {exBang.Message}");
                    }
                    chiSoTienTrinh++;
                    if (chiSoTienTrinh <= prb_TienTrinhChuyenGiao.Maximum)
                    {
                        prb_TienTrinhChuyenGiao.Value = chiSoTienTrinh;
                    }
                }

                if (soBangThanhCong > 0)
                {
                    CapNhatTrangThaiHoatDong("Đang tối ưu và dọn dẹp cơ sở dữ liệu...");
                    await Task.Run(() => Module_ChuyenGiaoDuLieu.VacuumDatabase(_duongDanCSDL_Nguon));
                }

                if (danhSachLoi.Count == 0)
                {
                    CapNhatTrangThaiHoatDong("Nạp dữ liệu thành công.");
                    Module_NhatKy.GhiNhatKy(SessionInfo.TenTaiKhoan, "Nhập dữ liệu JSON", $"Đã nạp thành công {soBangThanhCong}/{tongSoBang} bảng vào CSDL: {Path.GetFileName(_duongDanCSDL_Nguon)} | {DateTime.Now}");
                    MessageBox.Show($"Đã nạp thành công {soBangThanhCong}/{tongSoBang} bảng.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    CapNhatTrangThaiHoatDong("Hoàn tất với một số lỗi.");
                    MessageBox.Show($"Đã nạp thành công {soBangThanhCong}/{tongSoBang} bảng.\n\n" + string.Join(Environment.NewLine, danhSachLoi), "Báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                await TaiDuLieuNenAsync();
            }
            catch (Exception ex)
            {
                CapNhatTrangThaiHoatDong("Tiến trình bị hủy do lỗi.");
                Module_NhatKy.GhiNhatKy(SessionInfo.TenTaiKhoan, "Lỗi Nhập dữ liệu JSON", $"Sự cố: {ex.Message} | {DateTime.Now}");
                MessageBox.Show(ex.Message, "Lỗi Di Trú", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _dangXuLyMigration = false;
                ThietLapTrangThaiTuongTacUI(true);
                await AnThanhTienTrinhAsync();
            }
        }

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
                rtb.SelectionColor = MauGiaTri;
                rtb.AppendText("Khi bấm Xuất hoặc Nhập dữ liệu, hệ thống sẽ tự động duyệt qua và xử lý toàn bộ các CSDL trong danh sách.\n");
                rtb.SelectionColor = rtb.ForeColor;
            }
            finally
            {
                ResumeDrawing(rtb);
            }
        }

        private static void AppendDongThongTin(RichTextBox rtb, string tieuDe, string giaTri)
        {
            rtb.SelectionColor = MauTieuDe;
            rtb.AppendText(tieuDe);
            rtb.SelectionColor = MauGiaTri;
            rtb.AppendText(giaTri);
        }

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

        private static void SuspendDrawing(Control control) => SendMessage(control.Handle, WM_SETREDRAW, false, IntPtr.Zero);
        private static void ResumeDrawing(Control control)
        {
            SendMessage(control.Handle, WM_SETREDRAW, true, IntPtr.Zero);
            control.Invalidate();
        }

        private async void Cbo_ChonCSDL_Nguon_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_dangXuLyMigration) return;
            int index = cbo_ChonCSDL_Nguon.SelectedIndex;
            if (index < 0) return;

            string itemChon = cbo_ChonCSDL_Nguon.SelectedItem?.ToString() ?? "";

            checkBox1_ChonTatCaCacCSDL.CheckedChanged -= CheckBox1_ChonTatCaCacCSDL_CheckedChanged;
            try
            {
                checkBox1_ChonTatCaCacCSDL.Checked = (itemChon == LUA_CHON_TAT_CA);
            }
            finally
            {
                checkBox1_ChonTatCaCacCSDL.CheckedChanged += CheckBox1_ChonTatCaCacCSDL_CheckedChanged;
            }

            if (itemChon == LUA_CHON_TAT_CA)
            {
                HienThiThongTinChonTatCa();
                LamMoiCheckedListBoxAnToan(() => checkedListBox1_clb_DanhSachBang.Items.Clear());
                CapNhatTrangThaiHoatDong("Đã chọn tất cả cơ sở dữ liệu.");
                return;
            }

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
                Module_ChuyenGiaoDuLieu.InThongTinDatabaseLenRichTextBox(richTextBox1_ThongTinDatabaseDuocChon, duongDanFile, danhSachBang);
                CapNhatTrangThaiHoatDong("Đã nạp thông tin phân vùng dữ liệu.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Database Load] " + ex);
                if (!IsDisposed && !Disposing)
                {
                    Module_ChuyenGiaoDuLieu.InThongTinDatabaseLenRichTextBox(richTextBox1_ThongTinDatabaseDuocChon, duongDanFile, ex.Message);
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
                await Task.Yield();
                string thuMucTong = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Database-PhanMemThiDua2026");
                if (!Directory.Exists(thuMucTong))
                {
                    Directory.CreateDirectory(thuMucTong);
                }

                bool daKichHoatCuaSoCu = KichHoatCuaSoDaMoNeuCo(thuMucTong);
                if (daKichHoatCuaSoCu)
                {
                    CapNhatTrangThaiHoatDong("Đã có cửa sổ thư mục đang mở — đưa lên trên.");
                }
                else
                {
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
                        string duongDanCuaSoNay = ChuanHoaDuongDan(Uri.UnescapeDataString(new Uri(locationUrl).LocalPath));
                        if (!string.Equals(duongDanCuaSoNay, duongDanCanTim, StringComparison.OrdinalIgnoreCase))
                            continue;
                        IntPtr hwnd = new IntPtr((long)cuaSo.HWND);
                        if (hwnd == IntPtr.Zero) continue;
                        if (IsIconic(hwnd)) ShowWindowAsync(hwnd, SW_RESTORE);
                        SetForegroundWindow(hwnd);
                        return true;
                    }
                    catch { }
                    finally
                    {
                        if (cuaSo != null) Marshal.ReleaseComObject(cuaSo);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Shell COM Error]: " + ex.Message);
            }
            finally
            {
                if (danhSachCuaSo != null) Marshal.ReleaseComObject(danhSachCuaSo);
                if (shellApp != null) Marshal.ReleaseComObject(shellApp);
            }
            return false;
        }

        private static string ChuanHoaDuongDan(string duongDan) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(duongDan));

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

        private void HienThiThanhTienTrinh(int giaTriToiDa)
        {
            if (IsDisposed || !IsHandleCreated)
                return;
            try
            {
                prb_TienTrinhChuyenGiao.Visible = true;

                // ⭐ Chuyển sang kiểu Marquee để thanh tiến trình chạy liên tục, 
                // không bị đứng yên ở khoảng trống phía trước.
                prb_TienTrinhChuyenGiao.Style = ProgressBarStyle.Marquee;

                // Tốc độ chạy của hiệu ứng (số mili-giây / bước chạy, số càng nhỏ chạy càng nhanh mượt)
                prb_TienTrinhChuyenGiao.MarqueeAnimationSpeed = 30;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[HienThiThanhTienTrinh] " + ex.Message);
            }
        }

        private async Task AnThanhTienTrinhAsync()
        {
            try
            {
                await Task.Delay(1000);
                if (IsDisposed || !IsHandleCreated || _dangXuLyMigration) return;
                BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (IsDisposed) return;
                        prb_TienTrinhChuyenGiao.Value = 0;
                        prb_TienTrinhChuyenGiao.Visible = false;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("[AnThanhTienTrinhAsync] " + ex.Message);
                    }
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[AnThanhTienTrinhAsync] " + ex.Message);
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

        private void kryptonButton_Dong_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_dangXuLyMigration)
                {
                    MessageBox.Show("Hệ thống đang xử lý dữ liệu nền an toàn.\nVui lòng chờ hoàn tất trước khi đóng.", "Đang xử lý dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                try
                {
                    if (_boHuyLuong_Quet != null && !_boHuyLuong_Quet.IsCancellationRequested)
                    {
                        _boHuyLuong_Quet.Cancel();
                    }
                    if (_ctsTaiThongTinDatabase != null && !_ctsTaiThongTinDatabase.IsCancellationRequested)
                    {
                        _ctsTaiThongTinDatabase.Cancel();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[Cancel CTS] " + ex.Message);
                }

                Form12? form12 = Application.OpenForms["Form12"] as Form12;
                if (form12 != null)
                {
                    if (!form12.Visible) form12.Show();
                    if (form12.WindowState == FormWindowState.Minimized) form12.WindowState = FormWindowState.Normal;
                    form12.BringToFront();
                    form12.Activate();
                }
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[kryptonButton_Dong_Click] " + ex);
                try { Close(); } catch { }
            }
        }
    }
}