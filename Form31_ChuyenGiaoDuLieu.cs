using System.Diagnostics;

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
        private readonly Dictionary<string, string> _anhXaTenDatabase = new(StringComparer.OrdinalIgnoreCase)
        {
            { "csdl1", "Database_1 (Danh tính & Cấu hình)" },
            { "csdl2", "Database_2 (Thông tin chung)" },
            { "csdl3", "Database_3 (Nhật ký hệ thống)" },
            { "csdl4", "Database_4 (Dữ liệu nghiệp vụ Thi đua)" }
        };
        private int _dangTaiDuLieuFlag = 0;
        public Form31_ChuyenGiaoDuLieu()
        {
            InitializeComponent();
            KhoiTaoFormCauHinh();
            DangKyChuoiSuKien();
            InitToolTips();
        }
        private void Form31_ChuyenGiaoDuLieu_Load(object sender, EventArgs e)
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
            toolTip1.ToolTipTitle = "Gợi ý đăng nhập";
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
        //private void checkedListBox1_clb_DanhSachBang_MouseDown(object? sender, MouseEventArgs e)
        //{
        //    if (_dangCapNhatCheckedListBox)
        //        return;

        //    if (_dangXuLyClickCheckedList)
        //        return;

        //    try
        //    {
        //        _dangXuLyClickCheckedList = true;

        //        if (sender is not CheckedListBox clb)
        //            return;

        //        int index =
        //            clb.IndexFromPoint(e.Location);

        //        if (index < 0)
        //            return;

        //        // =============================================
        //        // KIỂM TRA CLICK VÀO CHECKBOX ?
        //        // =============================================

        //        Rectangle itemRect =
        //            clb.GetItemRectangle(index);

        //        Rectangle checkboxRect =
        //            new Rectangle(
        //                itemRect.X,
        //                itemRect.Y,
        //                20,
        //                itemRect.Height
        //            );

        //        bool clickVaoCheckbox =
        //            checkboxRect.Contains(e.Location);

        //        // =============================================
        //        // NẾU CLICK VÀO TEXT
        //        // => TỰ TOGGLE
        //        // =============================================

        //        if (!clickVaoCheckbox)
        //        {
        //            bool dangChecked =
        //                clb.GetItemChecked(index);

        //            clb.SetItemChecked(
        //                index,
        //                !dangChecked
        //            );
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(
        //            "[CheckedListBox UX Error] " + ex.Message
        //        );
        //    }
        //    finally
        //    {
        //        _dangXuLyClickCheckedList = false;
        //    }
        //}
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

            // =================================================
            // CHECKED LIST ENGINE - CẬP NHẬT ĐĂNG KÝ SỰ KIỆN UX MỚI
            // =================================================
            // Thay thế sự kiện MouseDown cũ bằng sự kiện thay đổi lựa chọn dòng chữ
            checkedListBox1_clb_DanhSachBang.SelectedIndexChanged += checkedListBox1_clb_DanhSachBang_SelectedIndexChanged;

            checkedListBox1_clb_DanhSachBang.ItemCheck += checkedListBox1_clb_DanhSachBang_ItemCheck;
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
                // ====================================================================
                // ⭐ BƯỚC CẢI TIẾN UX CHUẨN KỸ SƯ: TỰ ĐỘNG CHỌN DATABASE ĐẦU TIÊN
                // ====================================================================
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
                List<string> danhSachLoi = new(); // Bộ nhớ lưu trữ lỗi cục bộ

                foreach (var item in checkedListBox1_clb_DanhSachBang.CheckedItems)
                {
                    string tenBang = item.ToString()!;

                    // Tính % hiện tại
                    int phanTram = (int)Math.Round((double)chiSoTienTrinh / tongSoBang * 100);
                    CapNhatTrangThaiHoatDong($"{phanTram}% | Đang kết xuất cấu trúc bảng: {tenBang}...");

                    try
                    {
                        await Task.Run(() => Module_ChuyenGiaoDuLieu.XuatDuLieuRaJson(_duongDanCSDL_Nguon, tenBang, thuMucGoiCon));
                        soBangThanhCong++;
                    }
                    catch (Exception exLoiCucBo)
                    {
                        // Nếu bảng này lỗi, ghi nhận lại và ĐI TIẾP bảng sau, không crash ứng dụng
                        danhSachLoi.Add($"- Bảng [{tenBang}]: {exLoiCucBo.Message}");
                    }

                    chiSoTienTrinh++;
                    prb_TienTrinhChuyenGiao.Value = chiSoTienTrinh;
                }

                // BÁO CÁO TỔNG HỢP SAU KHI CHẠY XONG VÒNG LẶP
                if (danhSachLoi.Count == 0)
                {
                    CapNhatTrangThaiHoatDong("100% | Xuất dữ liệu an toàn thành công!");
                    MessageBox.Show($"Đã xuất thành công {soBangThanhCong}/{tongSoBang} bảng vào thư mục:\nDesktop\\Database-PhanMemThiDua2026\\{tenThuMucRieng}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    CapNhatTrangThaiHoatDong("Hoàn tất với một số cảnh báo.");
                    string tbLoi = $"Kết xuất hoàn tất một phần.\nThành công: {soBangThanhCong}/{tongSoBang} bảng.\n\nCác bảng sau bị lỗi (đã được bỏ qua):\n" + string.Join("\n", danhSachLoi);
                    MessageBox.Show(tbLoi, "Báo cáo Kết xuất Dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
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
                CapNhatTrangThaiHoatDong(
                    "Tiến trình bị hủy do lỗi.");

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
        private async void Cbo_ChonCSDL_Nguon_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_dangXuLyMigration)
                return;

            int index = cbo_ChonCSDL_Nguon.SelectedIndex;

            if (index < 0)
                return;

            if (index >= _danhSachDuongDanThucTe.Count)
                return;

            string duongDanFile = _danhSachDuongDanThucTe[index];

            _duongDanCSDL_Nguon = duongDanFile;

            int version = Interlocked.Increment(ref _phienTaiDuLieu);

            try
            {
                ThietLapTrangThaiTuongTacUI(false);

                CapNhatTrangThaiHoatDong("Đang phân tích cơ sở dữ liệu...");

                richTextBox1_ThongTinDatabaseDuocChon.Clear();

                List<string> danhSachBang =
                    await Task.Run(() =>
                    {
                        return Module_ChuyenGiaoDuLieu
                            .LayDanhSachBang(duongDanFile);
                    });

                if (version != _phienTaiDuLieu)
                    return;

                if (IsDisposed || Disposing)
                    return;

                // ====================================================================
                // 🔒 TRẠM AN TOÀN HỆ THỐNG: Ngắt liên kết sự kiện trước khi làm sạch danh sách
                // ====================================================================
                _dangCapNhatCheckedListBox = true;
                checkedListBox1_clb_DanhSachBang.SelectedIndexChanged -= checkedListBox1_clb_DanhSachBang_SelectedIndexChanged;

                checkedListBox1_clb_DanhSachBang.BeginUpdate(); // Chặn Windows vẽ lại giao diện liên tục gây nhấp nháy

                try
                {
                    checkedListBox1_clb_DanhSachBang.Items.Clear();

                    foreach (string tenBang in danhSachBang)
                    {
                        int idx = checkedListBox1_clb_DanhSachBang.Items.Add(tenBang);
                        checkedListBox1_clb_DanhSachBang.SetItemChecked(idx, true);
                    }
                }
                finally
                {
                    checkedListBox1_clb_DanhSachBang.EndUpdate(); // Cho phép giao diện vẽ lại một lần duy nhất

                    // Khôi phục lại liên kết sự kiện sau khi nạp dữ liệu sạch hoàn tất
                    checkedListBox1_clb_DanhSachBang.SelectedIndexChanged += checkedListBox1_clb_DanhSachBang_SelectedIndexChanged;
                    _dangCapNhatCheckedListBox = false;
                }
                // ====================================================================

                Module_ChuyenGiaoDuLieu
                    .InThongTinDatabaseLenRichTextBox(
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
                    Module_ChuyenGiaoDuLieu
                        .InThongTinDatabaseLenRichTextBox(
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
        // ⭐ SỬA LỖI BIÊN DỊCH VÀ ĐỒNG BỘ HIỂN THỊ
        private async void kryptonButton2_MoThuMuc_Click(object? sender, EventArgs e)
        {
            if (_dangTaiDuLieu || _dangXuLyMigration) return;

            try
            {
                kryptonButton2_MoThuMuc.Enabled = false;
                CapNhatTrangThaiHoatDong("Đang khởi động Windows Explorer...");
                await Task.Yield();

                string thuMucTong = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Database-PhanMemThiDua2026");
                if (!Directory.Exists(thuMucTong)) Directory.CreateDirectory(thuMucTong);

                await Task.Run(() =>
                {
                    try
                    {
                        ProcessStartInfo thongTinTienTrinh = new()
                        {
                            FileName = "explorer.exe",
                            Arguments = $"\"{thuMucTong}\"",
                            UseShellExecute = true,
                            ErrorDialog = false
                        };
                        Process.Start(thongTinTienTrinh);
                    }
                    catch (Exception ex) { Debug.WriteLine("[Explorer Error]: " + ex.Message); }
                });

                CapNhatTrangThaiHoatDong("Sẵn sàng.");
            }
            finally
            {
                kryptonButton2_MoThuMuc.Enabled = true;
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
                 object sender,
                 EventArgs e
             )
        {
            try
            {
                // =================================================
                // CHỐNG ĐÓNG KHI ĐANG MIGRATION
                // =================================================

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

                // =================================================
                // YÊU CẦU HỦY TASK NỀN
                // KHÔNG DISPOSE TẠI ĐÂY
                // =================================================

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

                // =================================================
                // KHÔI PHỤC FORM12
                // =================================================

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

                // =================================================
                // ĐÓNG FORM
                // =================================================

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
