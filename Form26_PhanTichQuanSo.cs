///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// Phân tích thành phần quân số theo đơn vị: bảng thống kê + biểu đồ cột 3D.
    /// Form6 vẫn gọi LoadDataTuDatabase() như cũ (không cần sửa Form6); việc đọc CSDL giờ chạy ở luồng nền,
    /// lần gọi mới tự hủy lần gọi cũ, kết quả cũ không bao giờ ghi đè kết quả mới.
    /// </summary>
    public partial class Form26_PhanTichQuanSo : Form
    {
        private static string DuongDanCsdl2 => Module_DanduongGPS.DuongDanCSDL2;   // đọc mỗi lần: CSDL có thể bị khôi phục/đổi đường dẫn
        private static readonly Font HeaderFont = new Font(Module_HeThong.TenFontHeThong, 10.5F, FontStyle.Bold);
        private static readonly Font CellFont = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Regular);
        private static readonly Font CellFontDam = new Font(Module_HeThong.TenFontHeThong, 10F, FontStyle.Bold);
        private readonly BieuDoCot3DF26? _bieuDo;
        private CancellationTokenSource? _ctsTai;
        private int _phienTai;   // số phiên: chỉ kết quả của phiên mới nhất được hiển thị
        public Form26_PhanTichQuanSo()
        {
            InitializeComponent();   // Designer đã nối Load + Paint + nút Đóng -> KHÔNG nối lại (bản cũ nối 2 lần)
            Module_BieuTuongNut.ApDung(this);   // biểu tượng nút: Hiện đại = tự vẽ, Cổ điển = ảnh gốc nhúng trong exe
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ShowInTaskbar = false;
            InitToolTips();
            Module_HeThong.UIHelper.EnableDoubleBuffer(tableLayoutPanel2);
            _bieuDo = Module_GiaoDienForm26Web.ApDung(this);   // giao diện kiểu web + biểu đồ 3D
        }
        private void Form26_PhanTichQuanSo_Load(object? sender, EventArgs e)
        {
            CauHinhLuoi();
            CapNhatPhienBanPhanMem();
            label_DonVi.Text = "ĐƠN VỊ: " + Module_TaiKhoan.XacDinhTenTieuDoan();
            statusStrip1.SizingGrip = false;
        }
        // Giữ nguyên chữ ký cũ: Form6 gọi hàm này (đồng bộ) mỗi lần mở trang. Bên trong chạy nền, trả về ngay.
        public void LoadDataTuDatabase() => _ = TaiDuLieuAsync();
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _ctsTai?.Cancel();
            _ctsTai?.Dispose();
            _ctsTai = null;
            base.OnFormClosed(e);
        }
        // =====================================================================================
        //  NẠP DỮ LIỆU
        // =====================================================================================
        private sealed record KetQuaPhanTich(DataTable Bang, List<BieuDoCot3DF26.Dong> DongBieuDo, int Tong, int SyQuan, int HaSyQuan, int ChienSi);
        public async Task TaiDuLieuAsync()
        {
            if (IsDisposed) return;
            _ctsTai?.Cancel();
            _ctsTai?.Dispose();
            var cts = _ctsTai = new CancellationTokenSource();
            var token = cts.Token;
            int phien = ++_phienTai;
            toolStripStatusLabel1.Text = "Đang tải dữ liệu...";
            CapNhatPhienBanPhanMem();   // form được giữ lại (Hide) giữa các lần mở -> nhãn phiên bản phải đọc lại mỗi lần
            try
            {
                string db = DuongDanCsdl2;
                List<string> thuTuDonVi = Module_DonVi.GetDanhSachDonVi();   // lấy ở luồng giao diện như bản cũ (mô-đun có thể dùng RAM chung)
                var kq = await Task.Run(() => DocVaPhanTich(db, thuTuDonVi, token), token);
                if (IsDisposed || phien != _phienTai) return;   // đã có lần tải mới hơn / form đã đóng
                HienThi(kq);
            }
            catch (OperationCanceledException) { /* bị lần tải mới thay thế */ }
            catch (Exception ex)
            {
                Debug.WriteLine("Form26 lỗi tải: " + ex.Message);
                if (!IsDisposed && phien == _phienTai) toolStripStatusLabel1.Text = "Không đọc được dữ liệu quân số";
            }
        }
        private static KetQuaPhanTich DocVaPhanTich(string db, List<string> thuTuDonVi, CancellationToken token)
        {
            var bang = TaoBangPhanTich();
            var dsBieuDo = new List<BieuDoCot3DF26.Dong>();
            int tong = 0, sy = 0, ha = 0, cs = 0;
            if (string.IsNullOrWhiteSpace(db) || !File.Exists(db))
            {
                ThemDongTongCong(bang, 0, 0, 0, 0);
                return new KetQuaPhanTich(bang, dsBieuDo, 0, 0, 0, 0);
            }
            var thongKe = new Dictionary<string, (int tong, int sy, int ha, int cs)>(StringComparer.OrdinalIgnoreCase);
            var cacheDonVi = new Dictionary<string, string>();   // chống giải mã AES trùng lặp
            var cacheCapBac = new Dictionary<string, string>();
            using (var conn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(db, chiDoc: true)))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT DonVi, CapBac FROM {Module_TaiKhoan.BangDanhSach}";
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    token.ThrowIfCancellationRequested();
                    string rawDv = rd.IsDBNull(0) ? "" : rd.GetString(0);
                    string rawCb = rd.IsDBNull(1) ? "" : rd.GetString(1);
                    if (!cacheDonVi.TryGetValue(rawDv, out string? dv))
                    {
                        dv = SafeGiaiMa(rawDv);
                        cacheDonVi[rawDv] = dv;
                    }
                    if (dv.Length == 0) continue;
                    if (!cacheCapBac.TryGetValue(rawCb, out string? cb))
                    {
                        cb = SafeGiaiMa(rawCb).ToUpperInvariant();
                        cacheCapBac[rawCb] = cb;
                    }
                    thongKe.TryGetValue(dv, out var d);
                    d.tong++;
                    if (cb is "H1" or "B1" or "B2") d.cs++;                  // chiến sĩ
                    else if (cb is "H2" or "H3") d.ha++;                      // hạ sĩ quan
                    else if (cb.StartsWith('T') || cb.StartsWith('U')) d.sy++; // sĩ quan
                    thongKe[dv] = d;
                }
            }
            int stt = 1;
            bang.BeginLoadData();
            // Đơn vị đã cấu hình (đúng thứ tự ưu tiên) trước, sau đó là đơn vị CÓ NGƯỜI nhưng chưa có trong danh sách đơn vị
            // (vd: dữ liệu Tân binh nhập với tên đơn vị khác DanhSach_DonVi_TanBinh) -> quân số không bị "mất" khỏi bảng.
            var daCo = new HashSet<string>(thuTuDonVi, StringComparer.OrdinalIgnoreCase);
            var thuTuDayDu = thuTuDonVi
                .Concat(thongKe.Keys.Where(k => !daCo.Contains(k)).OrderBy(k => k, StringComparer.CurrentCultureIgnoreCase))
                .ToList();
            foreach (string donVi in thuTuDayDu)
            {
                if (!thongKe.TryGetValue(donVi, out var k) || k.tong <= 0) continue;
                tong += k.tong; sy += k.sy; ha += k.ha; cs += k.cs;
                // 0 -> DBNull để ô để trống cho sạch mắt
                bang.Rows.Add(stt++, donVi, k.tong,
                    k.sy == 0 ? DBNull.Value : k.sy,
                    k.ha == 0 ? DBNull.Value : k.ha,
                    k.cs == 0 ? DBNull.Value : k.cs);
                dsBieuDo.Add(new BieuDoCot3DF26.Dong(donVi, k.sy, k.ha, k.cs));
            }
            ThemDongTongCong(bang, tong, sy, ha, cs);
            bang.EndLoadData();
            return new KetQuaPhanTich(bang, dsBieuDo, tong, sy, ha, cs);
        }
        // Dữ liệu cũ chưa mã hóa vẫn đọc được: giải mã rỗng thì dùng nguyên văn
        private static string SafeGiaiMa(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string s;
            try { s = Module_BaoMatAES.GiaiMa(raw)?.Trim() ?? ""; } catch { s = ""; }
            return s.Length == 0 ? raw.Trim() : s;
        }
        private void HienThi(KetQuaPhanTich kq)
        {
            var grid = kryptonDataGridView1;
            grid.SuspendLayout();
            try
            {
                var cu = grid.DataSource as DataTable;
                grid.DataSource = kq.Bang;
                cu?.Dispose();
                DinhCotSauKhiGan();
            }
            finally { grid.ResumeLayout(); }
            toolStripStatusLabel1.Text = $"Tổng quân số: {kq.Tong} {Module_HeThong.Tu_dong_chi}.";
            _bieuDo?.DatDuLieu(kq.DongBieuDo);
        }
        private static DataTable TaoBangPhanTich() => new DataTable
        {
            Columns =
            {
                new DataColumn("STT", typeof(int)),
                new DataColumn("DonVi", typeof(string)),
                new DataColumn("TongQuanSo", typeof(int)),
                new DataColumn("SyQuan", typeof(int)),
                new DataColumn("HaSyQuan", typeof(int)),
                new DataColumn("ChienSiNghiaVu", typeof(int))
            }
        };
        private static void ThemDongTongCong(DataTable dt, int tong, int sy, int ha, int cs)
        {
            var r = dt.NewRow();
            r["STT"] = DBNull.Value;
            r["DonVi"] = "Tổng cộng";
            r["TongQuanSo"] = tong;
            r["SyQuan"] = sy;
            r["HaSyQuan"] = ha;
            r["ChienSiNghiaVu"] = cs;
            dt.Rows.Add(r);
        }
        // =====================================================================================
        //  LƯỚI
        // =====================================================================================
        // Thuộc tính tĩnh: đặt 1 lần (không đặt lại mỗi lần nạp dữ liệu như bản cũ)
        private void CauHinhLuoi()
        {
            var grid = kryptonDataGridView1;
            if (grid == null || grid.IsDisposed) return;
            grid.SuspendLayout();
            try
            {
                typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(grid, true, null);
                grid.ReadOnly = true;
                grid.AllowUserToAddRows = false;
                grid.AllowUserToDeleteRows = false;
                grid.AllowUserToResizeRows = false;
                grid.AllowUserToResizeColumns = false;
                grid.MultiSelect = false;
                grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                grid.BorderStyle = BorderStyle.None;
                grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                grid.GridColor = Color.FromArgb(235, 238, 243);
                grid.BackgroundColor = Color.White;
                grid.RowHeadersVisible = false;
                grid.EnableHeadersVisualStyles = false;
                grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
                grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                grid.ColumnHeadersHeight = LogicalToDeviceUnits(44);
                var hs = grid.ColumnHeadersDefaultCellStyle;
                hs.Font = HeaderFont;
                hs.Alignment = DataGridViewContentAlignment.MiddleCenter;
                hs.BackColor = Color.FromArgb(245, 243, 255);
                hs.ForeColor = Color.FromArgb(76, 29, 149);
                hs.SelectionBackColor = hs.BackColor;
                hs.SelectionForeColor = hs.ForeColor;
                hs.WrapMode = DataGridViewTriState.True;
                var cs = grid.DefaultCellStyle;
                cs.Font = CellFont;
                cs.ForeColor = Color.FromArgb(30, 41, 59);
                cs.BackColor = Color.White;
                cs.Alignment = DataGridViewContentAlignment.MiddleCenter;
                cs.SelectionBackColor = Color.FromArgb(237, 233, 254);
                cs.SelectionForeColor = Color.FromArgb(30, 41, 59);
                cs.Padding = new Padding(3);
                grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 253);
                grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = cs.SelectionBackColor;
                grid.RowTemplate.Height = LogicalToDeviceUnits(34);
            }
            finally { grid.ResumeLayout(); }
        }
        private void DinhCotSauKhiGan()
        {
            var grid = kryptonDataGridView1;
            foreach (DataGridViewColumn col in grid.Columns) col.SortMode = DataGridViewColumnSortMode.NotSortable;
            if (grid.Columns.Contains("STT"))
            {
                var c = grid.Columns["STT"]!;
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c.Width = c.MinimumWidth = LogicalToDeviceUnits(60);
                c.HeaderText = "STT";
            }
            if (grid.Columns.Contains("DonVi"))
            {
                var c = grid.Columns["DonVi"]!;
                c.HeaderText = "Đơn vị";
                c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                c.FillWeight = 160;
            }
            if (grid.Columns.Contains("TongQuanSo")) grid.Columns["TongQuanSo"]!.HeaderText = "Tổng quân số";
            if (grid.Columns.Contains("SyQuan")) grid.Columns["SyQuan"]!.HeaderText = Module_HeThong.Sy_Quan;
            if (grid.Columns.Contains("HaSyQuan")) grid.Columns["HaSyQuan"]!.HeaderText = Module_HeThong.Ha_Sy_Quan;
            if (grid.Columns.Contains("ChienSiNghiaVu")) grid.Columns["ChienSiNghiaVu"]!.HeaderText = Module_HeThong.Chien_Sy_Nghia_Vu;
            // Dòng "Tổng cộng" (dòng cuối): nền tím nhạt + chữ đậm
            if (grid.Rows.Count > 0)
            {
                var st = grid.Rows[grid.Rows.Count - 1].DefaultCellStyle;
                st.Font = CellFontDam;
                st.BackColor = Color.FromArgb(245, 243, 255);
                st.ForeColor = Color.FromArgb(76, 29, 149);
                st.SelectionBackColor = Color.FromArgb(237, 233, 254);
            }
            grid.ClearSelection();
        }
        // =====================================================================================
        //  NÚT / TRẠNG THÁI
        // =====================================================================================
        //private void btnThoat_Click(object? sender, EventArgs e) => DongVaQuayLaiForm6();
        private void kryptonButton1_Dong_Click(object? sender, EventArgs e) => DongVaQuayLaiForm6();
        private void DongVaQuayLaiForm6()
        {
            if (Parent is Panel panel)
            {
                var form6 = panel.Controls.OfType<Form6_XuLyData>().FirstOrDefault();
                if (form6 != null)
                {
                    form6.Show();
                    form6.BringToFront();
                }
            }
            Hide();
        }
        private void CapNhatPhienBanPhanMem()
        {
            if (IsDisposed || toolStripStatusLabel2 == null || toolStripStatusLabel2.IsDisposed) return;
            string doiTuong = "";
            try
            {
                string file = DuongDanCsdl2?.Trim() ?? "";
                if (file.Length > 0 && File.Exists(file))
                {
                    using var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(file, chiDoc: true, cacheChung: true));
                    cn.Open();
                    using var cmd = cn.CreateCommand();
                    cmd.CommandText = "SELECT DoiTuong FROM PhienBan_DoiTuong WHERE ID = 1 LIMIT 1;";
                    string ma = cmd.ExecuteScalar()?.ToString()?.Trim() ?? "";
                    if (ma.Length > 0)
                    {
                        try { doiTuong = Module_BaoMatAES.GiaiMa(ma)?.Trim() ?? ""; }
                        catch (Exception ex) { Debug.WriteLine($"[Lỗi giải mã AES]: {ex.Message}"); }
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"[Lỗi đọc phiên bản]: {ex.Message}"); }
            if (string.IsNullOrWhiteSpace(doiTuong)) doiTuong = "(không xác định)";
            else if (doiTuong.Length > 80) doiTuong = doiTuong.Substring(0, 77) + "...";
            toolStripStatusLabel2.Spring = true;
            toolStripStatusLabel2.TextAlign = ContentAlignment.MiddleRight;
            toolStripStatusLabel2.Text = $"Phần mềm: {doiTuong}";
        }
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Thao_Tac;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.SetToolTip(kryptonButton1_Dong, "Đóng cửa sổ này và quay lại màn hình trước!");
        }
        // Designer vẫn nối sự kiện này; biểu đồ nay là control riêng (BieuDoCot3DF26) nên không còn vẽ tay lên TableLayoutPanel.
        private void tableLayoutPanel2_Paint(object? sender, PaintEventArgs e) { }
    }
}