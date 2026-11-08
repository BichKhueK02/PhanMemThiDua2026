///Phiên bản 1
///Phiên bản 2

///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using Krypton.Toolkit;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form44_SoVangBaNhat : Form
    {
        // ===================== HẰNG / TÀI NGUYÊN DÙNG CHUNG =====================
        private const int GioiHanHienThiThanhTich = 25;   // lưới chỉ hiện 25 ký tự đầu của "Thành tích"

        // Font tạo 1 lần cho cả chương trình (không rò rỉ GDI handle theo mỗi lần mở form)
        private static readonly Font _fontRegular9 = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Regular);
        private static readonly Font _fontBold9 = new Font(Module_HeThong.TenFontHeThong, 9F, FontStyle.Bold);

        // Tìm PropertyInfo 1 lần (reflection khá tốn nếu gọi lặp lại)
        private static readonly System.Reflection.PropertyInfo? _pDoubleBuffered =
            typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        // Các cột được phép sửa trên form (SoHieu, PhanLoai chỉ-đọc nên KHÔNG ghi lại, tránh mã hóa lại làm lệch khóa so khớp)
        private static readonly string[] CotSua =
        {
            "HoVaTen", "NamSinh", "QueQuan", "NgayVaoCAND", "CapBac", "ChucVu", "DonVi",
            "GhiChu", "ThanhTich", "ThongBaoTrungDoan", "SoTTTrongSo", "ThangCongNhan"
        };

        // Đường dẫn CSDL luôn đọc mới từ module dẫn đường (không chụp cứng lúc dựng form)
        private static string DuongDanDb => Module_DanduongGPS.DuongDanCSDL2;

        // Tên bảng theo phiên bản phần mềm: chỉ có 2 giá trị cố định => không thể bị chèn SQL.
        // LayPhienBanPhanMem() mở SQLite + giải mã nên chỉ gọi 1 lần cho mỗi lượt nạp, không gọi lại ở mỗi lần đọc.
        private string? _bangSoVang;
        private string TenBangHienTai => _bangSoVang ??= ChonBangSoVang();
        private static string ChonBangSoVang()
        {
            string phienBan = Module_TaiKhoan.LayPhienBanPhanMem() ?? "";
            return phienBan.Contains("tân binh", StringComparison.OrdinalIgnoreCase)
                ? "DanhSachTanBinh_SoVangBaNhat"
                : "DanhSach_SoVangBaNhat";
        }

        private bool _daKhoiTaoToolTip;
        private bool _daCauHinhGrid;
        private int _cotThanhTich = -1;
        private CancellationTokenSource? _ctsLoad;

        // ===================== KHỞI TẠO =====================
        public Form44_SoVangBaNhat()
        {
            InitializeComponent();

            // Designer add statusStrip1 trước tableLayoutPanel1 (Dock = Fill) nên bảng bị thanh trạng thái đè mất ~26px đáy.
            // Đưa vùng Fill lên trước để WinForms dock thanh trạng thái trước, bảng chiếm phần còn lại.
            tableLayoutPanel1.BringToFront();

            kryptonTextBox1_TimKiemTheoTen.CueHint.CueHintText = Module_HeThong.Nhap_Tim_Kiem;
            GiaoDienForm42_44Web.ApDung(this);
            Module_HeThong.BocBoTronGiaoDien(richTextBox1_ThanhTich);

            Shown += Form44_SoVangBaNhat_Shown;
            Disposed += (s, e) => GiaiPhongTaiNguyen();

            // Bộ lọc: gõ tên thì lọc trễ 150ms (gõ nhanh không giật), đổi đơn vị thì lọc ngay.
            // Mỗi sự kiện chỉ gắn đúng 1 lần ở đây (không gắn lại trong các nút làm mới).
            kryptonTextBox1_TimKiemTheoTen.TextChanged += TimKiemTheoTen_TextChanged;
            comboBox_TimKiemDonVi.SelectedIndexChanged += DonVi_SelectedIndexChanged;

            // Hiện/ẩn nút Lưu, Xóa theo việc đã chọn người hay chưa
            kryptonTextBox1_SoHieu.TextChanged += (s, e) => CapNhatTrangThaiNut();
            kryptonTextBox1_HoVaTen.TextChanged += (s, e) => CapNhatTrangThaiNut();

            // Sự kiện của lưới: gắn 1 lần
            kryptonDataGridView1.CellFormatting += kryptonDataGridView1_CellFormatting;
            kryptonDataGridView1.CellMouseClick += kryptonDataGridView1_CellMouseClick;
            kryptonDataGridView1.CellClick += kryptonDataGridView1_CellClick;
        }

        private void GiaiPhongTaiNguyen()
        {
            try
            {
                _timerLoc?.Stop();
                _timerLoc?.Dispose();
                _timerLoc = null;
                _ctsLoad?.Cancel();
                _ctsLoad?.Dispose();
                _ctsLoad = null;
            }
            catch { /* dọn dẹp best-effort */ }
        }

        private void Form44_SoVangBaNhat_Load(object? sender, EventArgs e)
        {
            try
            {
                CauHinhKryptonTextBoxChiDoc(kryptonTextBox1_STT);
                CauHinhKryptonTextBoxChiDoc(kryptonTextBox1_SoHieu);
                CauHinhKryptonTextBoxChiDoc(kryptonTextBox1_PhanLoai);

                kryptonDataGridView1.ContextMenuStrip = contextMenuStrip1;
                kryptonDataGridView1.Dock = DockStyle.Fill;
                CauHinhGridMotLan();            // dựng cột + style 1 lần
                CapNhatTrangThaiNut();
                Module_MenuChuotPhai.TichHopGiaoDien(contextMenuStrip1);
                InitToolTips();
                // KHÔNG nạp dữ liệu ở đây: Form55.MoForm44Async lo việc đó
            }
            catch (Exception ex) { Debug.WriteLine("[Form44.Load] " + ex); }
        }

        private void Form44_SoVangBaNhat_Shown(object? sender, EventArgs e) => kryptonTextBox1_TimKiemTheoTen.Focus();

        // ===================== TIỆN ÍCH CSDL =====================
        // Pooling=false: đóng kết nối là nhả hẳn tệp .db (không làm kẹt sao lưu/khôi phục), giống Module_BaNhat.
        private static SqliteConnection MoKetNoi(bool chiDoc)
        {
            var sb = new SqliteConnectionStringBuilder
            {
                DataSource = DuongDanDb,
                Mode = chiDoc ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite,
                Pooling = false
            };
            var cn = new SqliteConnection(sb.ToString());
            try { cn.Open(); return cn; }
            catch { cn.Dispose(); throw; }
        }

        private static string SafeGiaiMa(string? cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText)) return "";
            try { return Module_BaoMatAES.GiaiMa(cipherText).Trim(); }
            catch { return "[Lỗi giải mã]"; }
        }

        private static void BaoForm55CanNapLai()
            => Application.OpenForms.OfType<Form55_QuanLyHeThongThiDuaBaNhat>().FirstOrDefault()?.DanhDauCanNapLai();

        // ===================== LƯỚI =====================
        private void CauHinhGridMotLan()
        {
            if (_daCauHinhGrid) return;
            var g = kryptonDataGridView1;
            _pDoubleBuffered?.SetValue(g, true, null);
            g.AutoGenerateColumns = false;   // quan trọng: không sinh lại cột mỗi lần đổi DataSource
            g.AllowUserToAddRows = false; g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false; g.AllowUserToOrderColumns = false;
            g.RowHeadersVisible = false; g.ReadOnly = true; g.MultiSelect = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            g.GridStyles.Style = Krypton.Toolkit.DataGridViewStyle.List;
            g.RowTemplate.Height = 36;
            g.ColumnHeadersHeight = 50;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            g.StateCommon.HeaderColumn.Content.Font = _fontBold9;
            g.StateCommon.DataCell.Content.Font = _fontRegular9;
            g.StateCommon.DataCell.Border.Color1 = Color.FromArgb(224, 224, 224);
            g.StateCommon.DataCell.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.All;
            g.StateCommon.DataCell.Border.Width = 1;
            g.StateSelected.DataCell.Back.Color1 = Color.FromArgb(232, 244, 253);
            g.StateSelected.DataCell.Content.Color1 = Color.FromArgb(0, 102, 204);

            var giua = DataGridViewContentAlignment.MiddleCenter;
            var trai = DataGridViewContentAlignment.MiddleLeft;
            g.Columns.AddRange(
                Cot("ID", "ID", 40, giua, hien: false),
                Cot("STT", "STT", 40, giua),
                Cot("HoVaTen", "Họ và tên", 180, trai),
                Cot("SoHieu", "Số hiệu", 80, giua),
                Cot("NamSinh", "Năm sinh", 85, giua),
                Cot("QueQuan", "Quê quán", 150, trai),
                Cot("NgayVaoCAND", "Vào CAND", 100, giua),
                Cot("CapBac", "Cấp bậc", 80, giua),
                Cot("ChucVu", "Chức vụ", 110, giua),
                Cot("DonVi", "Đơn vị", 80, giua),
                Cot("PhanLoai", "Phân loại", 80, giua, hien: false),
                Cot("GhiChu", "Ghi chú", 120, trai),
                Cot("ThanhTich", "Thành tích", 180, trai, fill: false),
                Cot("ThongBaoTrungDoan", "Thông báo E", 120, giua),
                Cot("SoTTTrongSo", "Số vào sổ", 90, giua),
                Cot("ThangCongNhan", "Tháng vào sổ", 160, giua));
            _cotThanhTich = g.Columns["ThanhTich"]!.Index;   // CellFormatting so sánh theo chỉ số, không so chuỗi mỗi ô vẽ
            _daCauHinhGrid = true;
        }

        private static DataGridViewTextBoxColumn Cot(string ten, string tieuDe, int rong,
            DataGridViewContentAlignment canh, bool hien = true, bool fill = true)
        {
            var c = new DataGridViewTextBoxColumn
            {
                Name = ten,
                DataPropertyName = ten,
                HeaderText = tieuDe,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = true,
                Visible = hien
            };
            c.DefaultCellStyle.Alignment = canh;
            if (fill) { c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; c.FillWeight = rong; c.MinimumWidth = 40; }
            else { c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; c.Width = rong; }
            return c;
        }

        private void kryptonDataGridView1_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != _cotThanhTich) return;
            if (e.Value is string s && s.Length > GioiHanHienThiThanhTich)
            {
                e.Value = s.Substring(0, GioiHanHienThiThanhTich) + " ...";
                e.FormattingApplied = true;
            }
        }

        private static DataTable TaoSchemaSoVang()
        {
            var dt = new DataTable { Locale = CultureInfo.InvariantCulture };
            dt.Columns.Add("ID", typeof(int));
            dt.Columns.Add("STT", typeof(int));
            foreach (var c in new[] { "HoVaTen", "SoHieu", "NamSinh", "QueQuan", "NgayVaoCAND", "CapBac", "ChucVu",
                                      "DonVi", "PhanLoai", "GhiChu", "ThanhTich", "ThongBaoTrungDoan", "SoTTTrongSo", "ThangCongNhan" })
                dt.Columns.Add(c, typeof(string));
            return dt;
        }

        // Đọc + giải mã ở luồng nền. Dòng đệm của CSDL (ID = -1) không phải người thật nên bỏ qua (khớp cách Module_BaNhat xử lý).
        private static DataTable DocSoVang(string tenBang, CancellationToken token)
        {
            var dt = TaoSchemaSoVang();
            using var cn = MoKetNoi(true);
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $@"SELECT ID, HoVaTen, SoHieu, NamSinh, QueQuan, NgayVaoCAND, CapBac, ChucVu, DonVi,
                                        PhanLoai, GhiChu, ThanhTich, ThongBaoTrungDoan, SoTTTrongSo, ThangCongNhan
                                 FROM [{tenBang}] WHERE ID <> -1 ORDER BY STT ASC, ID ASC";
            using var r = cmd.ExecuteReader();
            var buf = new object[16];
            int stt = 1;
            dt.BeginLoadData();
            try
            {
                while (r.Read())
                {
                    token.ThrowIfCancellationRequested();
                    buf[0] = r.GetInt32(0);
                    buf[1] = stt++;                       // STT hiển thị = thứ tự trong danh sách
                    for (int i = 1; i < 15; i++)
                        buf[i + 1] = r.IsDBNull(i) ? "" : SafeGiaiMa(r.GetString(i));
                    dt.Rows.Add(buf);
                }
            }
            finally { dt.EndLoadData(); }
            return dt;
        }

        /// <summary>Nạp lại lưới. Lần gọi mới hơn sẽ hủy lần cũ nên chỉ có kết quả mới nhất được ghi vào lưới.</summary>
        public async Task LoadDuLieuSoVangBaNhatAsync() => await NapLuoiAsync();

        private async Task<bool> NapLuoiAsync()
        {
            string db = DuongDanDb;
            if (string.IsNullOrWhiteSpace(db) || !File.Exists(db)) return false;

            var cu = _ctsLoad;
            var cts = _ctsLoad = new CancellationTokenSource();
            cu?.Cancel();
            cu?.Dispose();
            var token = cts.Token;

            try
            {
                CauHinhGridMotLan();
                // Đọc phiên bản + CSDL + giải mã AES: toàn bộ ở luồng nền, UI không bị chặn
                var (bang, dt) = await Task.Run(() =>
                {
                    string b = ChonBangSoVang();
                    return (b, DocSoVang(b, token));
                }, token);

                token.ThrowIfCancellationRequested();
                if (IsDisposed || kryptonDataGridView1.IsDisposed) return false;

                _bangSoVang = bang;
                var g = kryptonDataGridView1;
                TatVe(g);
                try { g.DataSource = dt; }
                finally { BatVe(g); }

                LamMoiBoLocSauKhiDoiDuLieu();
                return true;
            }
            catch (OperationCanceledException) { return false; }   // có lần nạp mới hơn -> bỏ qua im lặng
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi nạp bảng Sổ vàng: " + ex);
                return false;
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr h, int msg, int w, int l);
        private static void TatVe(Control c) { if (c.IsHandleCreated) SendMessage(c.Handle, 0x000B, 0, 0); }
        private static void BatVe(Control c) { if (c.IsHandleCreated) { SendMessage(c.Handle, 0x000B, 1, 0); c.Invalidate(true); } }

        private static DataRow? TimDong(DataTable dt, int id)
        {
            foreach (DataRow r in dt.Rows)
                if (r["ID"] is int v && v == id) return r;
            return null;
        }

        // ===================== Ô NHẬP / CHỌN DÒNG =====================
        private void CauHinhKryptonTextBoxChiDoc(KryptonTextBox textBox)
        {
            if (textBox == null || textBox.IsDisposed) return;
            textBox.ReadOnly = true;
            textBox.StateCommon.Back.Color1 = Color.LightGreen;
            textBox.StateCommon.Content.Color1 = Color.DarkGreen;
            textBox.StateCommon.Content.Font = _fontRegular9;
        }

        private void kryptonDataGridView1_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            // Chỉ xử lý chuột phải vào một dòng hợp lệ (chuột trái đã có CellClick)
            if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;
            var g = kryptonDataGridView1;
            if (e.RowIndex >= g.Rows.Count) return;
            try
            {
                g.ClearSelection();
                g.Rows[e.RowIndex].Selected = true;
                if (e.ColumnIndex >= 0 && g.Columns[e.ColumnIndex].Visible)
                    g.CurrentCell = g.Rows[e.RowIndex].Cells[e.ColumnIndex];
            }
            catch (Exception ex) { Debug.WriteLine("[CellMouseClick] " + ex.Message); }
            FillDataToControls(g.Rows[e.RowIndex]);
        }

        private void kryptonDataGridView1_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= kryptonDataGridView1.Rows.Count) return;
            FillDataToControls(kryptonDataGridView1.Rows[e.RowIndex]);
        }

        // Đọc theo nguồn dữ liệu (DataRowView luôn đủ cột kể cả cột ẩn), không đọc từng ô theo tên
        private static string Truong(DataGridViewRow row, string tenCot)
            => row.DataBoundItem is DataRowView v && v.Row.Table.Columns.Contains(tenCot)
                ? v[tenCot]?.ToString() ?? ""
                : "";

        private void FillDataToControls(DataGridViewRow row)
        {
            // Ép RichTextBox về null trước để diệt tận gốc chữ/định dạng cũ
            richTextBox1_ThanhTich.Text = null;

            kryptonTextBox1_STT.Tag = row.DataBoundItem is DataRowView v ? v["ID"] : null;   // ID dùng để lưu/xóa
            kryptonTextBox1_STT.Text = Truong(row, "STT");
            kryptonTextBox1_HoVaTen.Text = Truong(row, "HoVaTen");
            kryptonTextBox1_SoHieu.Text = Truong(row, "SoHieu");
            kryptonTextBox1_NamSinh.Text = Truong(row, "NamSinh");
            kryptonTextBox1_QueQuan.Text = Truong(row, "QueQuan");
            kryptonTextBox1_NgayVaoCAND.Text = Truong(row, "NgayVaoCAND");
            kryptonTextBox1_CapBac.Text = Truong(row, "CapBac");
            kryptonTextBox1_ChucVu.Text = Truong(row, "ChucVu");
            kryptonTextBox1_DonVi.Text = Truong(row, "DonVi");
            kryptonTextBox1_PhanLoai.Text = Truong(row, "PhanLoai");
            kryptonTextBox1_GhiChu.Text = Truong(row, "GhiChu");
            kryptonTextBox1_ThongBaoTrungDoan.Text = Truong(row, "ThongBaoTrungDoan");
            kryptonTextBox1_SoTTTrongSo.Text = Truong(row, "SoTTTrongSo");
            kryptonTextBox1_ThangCongNhan.Text = Truong(row, "ThangCongNhan");

            string thanhTich = Truong(row, "ThanhTich");
            if (thanhTich.Length > 0) richTextBox1_ThanhTich.Text = thanhTich;
        }

        private void ClearTextBoxes()
        {
            kryptonTextBox1_STT.Tag = null;
            kryptonTextBox1_STT.Clear();
            kryptonTextBox1_HoVaTen.Clear();
            kryptonTextBox1_SoHieu.Clear();
            kryptonTextBox1_NamSinh.Clear();
            kryptonTextBox1_QueQuan.Clear();
            kryptonTextBox1_NgayVaoCAND.Clear();
            kryptonTextBox1_CapBac.Clear();
            kryptonTextBox1_ChucVu.Clear();
            kryptonTextBox1_DonVi.Clear();
            kryptonTextBox1_PhanLoai.Clear();
            kryptonTextBox1_GhiChu.Clear();
            richTextBox1_ThanhTich.Clear();
            kryptonTextBox1_ThongBaoTrungDoan.Clear();
            kryptonTextBox1_SoTTTrongSo.Clear();
            kryptonTextBox1_ThangCongNhan.Clear();
        }

        private bool LayIdDangChon(out int id)
        {
            id = 0;
            return kryptonTextBox1_STT.Tag != null
                && int.TryParse(Convert.ToString(kryptonTextBox1_STT.Tag, CultureInfo.InvariantCulture), out id);
        }

        private void CapNhatTrangThaiNut()
        {
            bool coDuLieu = !string.IsNullOrWhiteSpace(kryptonTextBox1_SoHieu.Text) &&
                            !string.IsNullOrWhiteSpace(kryptonTextBox1_HoVaTen.Text);
            if (kryptonButton1_XoaCBCS.Visible != coDuLieu) kryptonButton1_XoaCBCS.Visible = coDuLieu;
            if (kryptonButton_LuuDataSoVang.Visible != coDuLieu) kryptonButton_LuuDataSoVang.Visible = coDuLieu;
        }

        // ===================== THANH TRẠNG THÁI =====================
        private void CapNhatThongKeSoLuong()
        {
            int tong = 0, hienThi = 0; bool dangLoc = false;
            if (kryptonDataGridView1.DataSource is DataTable dt)
            {
                tong = dt.Rows.Count;
                hienThi = dt.DefaultView.Count;
                dangLoc = dt.DefaultView.RowFilter.Length > 0;
            }
            string dc = Module_HeThong.Tu_dong_chi;
            if (tong == 0)
            {
                toolStripStatusLabel1_ThongBao.ForeColor = Color.Red;
                toolStripStatusLabel1_ThongBao.Text = $"Sổ vàng hiện chưa có {dc} nào đề nghị biểu dương phong trào thi đua \"Ba Nhất\" trong CAND";
            }
            else if (dangLoc)
            {
                toolStripStatusLabel1_ThongBao.ForeColor = Color.Blue;
                toolStripStatusLabel1_ThongBao.Text = $"Tổng quân số: {tong} {dc} | Kết quả tìm kiếm: {hienThi} {dc}";
            }
            else
            {
                toolStripStatusLabel1_ThongBao.ForeColor = Color.DarkBlue;
                toolStripStatusLabel1_ThongBao.Text = $"Tổng cộng: {tong} {dc}";
            }
        }

        // Hiện thông báo tạm rồi tự trả về dòng thống kê MÀ KHÔNG chặn luồng thao tác (bản cũ await Task.Delay làm nút bị khóa thêm).
        // Thông báo mới hơn sẽ vô hiệu hóa lượt trả về của thông báo cũ.
        private int _tokenThongBao;
        private async void ThongBaoTam(string noiDung, int ms = 1500)
        {
            try
            {
                int token = ++_tokenThongBao;
                toolStripStatusLabel1_ThongBao.ForeColor = Color.DarkBlue;
                toolStripStatusLabel1_ThongBao.Text = noiDung;
                await Task.Delay(ms);
                if (token == _tokenThongBao && !IsDisposed) CapNhatThongKeSoLuong();
            }
            catch (Exception ex) { Debug.WriteLine("[ThongBaoTam] " + ex.Message); }
        }

        // ===================== BỘ LỌC =====================
        private bool _dangDatLaiLoc;                       // true khi đang nạp/đặt lại bộ lọc bằng code -> chặn các lượt lọc lẻ tẻ
        private System.Windows.Forms.Timer? _timerLoc;     // debounce ô gõ tên

        private void TimKiemTheoTen_TextChanged(object? s, EventArgs e) => KhoiDongLocTre();
        private void DonVi_SelectedIndexChanged(object? s, EventArgs e) => LocDuLieu();

        private void KhoiDongLocTre()
        {
            if (_dangDatLaiLoc || IsDisposed) return;
            if (_timerLoc == null)
            {
                _timerLoc = new System.Windows.Forms.Timer { Interval = 150 };
                _timerLoc.Tick += (s, ev) => LocDuLieu();   // LocDuLieu tự dừng timer
            }
            _timerLoc.Stop();
            _timerLoc.Start();
        }

        private static string EscapeLike(string s)
        {
            var sb = new StringBuilder(s.Length + 8);
            foreach (char ch in s)
            {
                if (ch is '*' or '%' or '[' or ']') sb.Append('[').Append(ch).Append(']');
                else sb.Append(ch);
            }
            return sb.ToString();
        }

        private void LocDuLieu()
        {
            _timerLoc?.Stop();                              // hủy lượt lọc trễ đang chờ (nếu có)
            if (_dangDatLaiLoc || IsDisposed) return;
            if (kryptonDataGridView1.DataSource is not DataTable dt) return;

            var dk = new List<string>(2);
            string ten = kryptonTextBox1_TimKiemTheoTen.Text.Trim();
            if (ten.Length > 0)
                dk.Add($"HoVaTen LIKE '%{EscapeLike(ten.Replace("'", "''"))}%'");

            if (comboBox_TimKiemDonVi.SelectedIndex > 0 && comboBox_TimKiemDonVi.SelectedItem is string donVi)
                dk.Add($"DonVi = '{donVi.Replace("'", "''")}'");

            try { dt.DefaultView.RowFilter = string.Join(" AND ", dk); }
            catch (Exception ex) { Debug.WriteLine("[LOC] " + ex.Message); dt.DefaultView.RowFilter = string.Empty; }

            CapNhatThongKeSoLuong();
        }

        private void DatLaiBoLoc()
        {
            _timerLoc?.Stop();
            _dangDatLaiLoc = true;
            try
            {
                kryptonTextBox1_TimKiemTheoTen.Clear();
                if (comboBox_TimKiemDonVi.Items.Count > 0) comboBox_TimKiemDonVi.SelectedIndex = 0;
            }
            finally { _dangDatLaiLoc = false; }
            LocDuLieu();
        }

        // Dựng lại danh sách đơn vị từ dữ liệu hiện có rồi áp lại bộ lọc đúng 1 lần
        private void LamMoiBoLocSauKhiDoiDuLieu()
        {
            CapNhatDanhSachDonVi();
            LocDuLieu();
        }

        private void CapNhatDanhSachDonVi()
        {
            if (kryptonDataGridView1.DataSource is not DataTable dt) return;

            string? dangChon = comboBox_TimKiemDonVi.SelectedIndex > 0
                ? comboBox_TimKiemDonVi.SelectedItem as string : null;

            var coTrongData = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in dt.Rows)
            {
                string v = (r["DonVi"] as string)?.Trim() ?? "";
                if (v.Length > 0) coTrongData.Add(v);
            }

            _dangDatLaiLoc = true;
            try
            {
                comboBox_TimKiemDonVi.BeginUpdate();
                try
                {
                    comboBox_TimKiemDonVi.Items.Clear();
                    comboBox_TimKiemDonVi.Items.Add(Module_HeThong.Tat_Ca);
                    // Đúng thứ tự của danh sách chuẩn, chỉ giữ đơn vị có dữ liệu
                    foreach (string chuan in Module_DonVi.GetDanhSachDonVi())
                        if (coTrongData.Remove(chuan)) comboBox_TimKiemDonVi.Items.Add(chuan);
                    // Đơn vị gõ sai tên (không khớp danh sách chuẩn) vẫn đưa xuống cuối để không mất khả năng lọc
                    foreach (string thua in coTrongData) comboBox_TimKiemDonVi.Items.Add(thua);

                    int vt = dangChon == null ? 0 : comboBox_TimKiemDonVi.Items.IndexOf(dangChon);
                    comboBox_TimKiemDonVi.SelectedIndex = vt >= 0 ? vt : 0;
                }
                finally { comboBox_TimKiemDonVi.EndUpdate(); }
            }
            finally { _dangDatLaiLoc = false; }
        }

        private void kryptonButton_LamMoiCacOTimKiem_Click(object? sender, EventArgs e) => DatLaiBoLoc();

        // ===================== TOOLTIP =====================
        private void InitToolTips()
        {
            if (_daKhoiTaoToolTip) return;
            try
            {
                toolTip1.IsBalloon = true;
                toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
                toolTip1.ToolTipIcon = ToolTipIcon.Info;
                toolTip1.InitialDelay = 300;
                toolTip1.AutoPopDelay = 2500;
                toolTip1.ReshowDelay = 100;
                toolTip1.ShowAlways = true;

                toolTip1.SetToolTip(kryptonButton_LamMoiCacOTimKiem, "Xóa nội dung tìm kiếm và đặt lại các bộ lọc về trạng thái mặc định");
                toolTip1.SetToolTip(kryptonButton_RefershCSDL, "Tải lại toàn bộ dữ liệu mới nhất từ cơ sở dữ liệu và làm mới trang");
                toolTip1.SetToolTip(kryptonButton1_XoaCBCS, "Xóa thông tin cán bộ chiến sĩ đang chọn khỏi danh sách Sổ vàng Ba Nhất");
                toolTip1.SetToolTip(kryptonButton_LuuDataSoVang, "Lưu hoặc cập nhật thông tin dữ liệu Sổ vàng của cán bộ vào cơ sở dữ liệu");
                toolTip1.SetToolTip(kryptonButton1_Thoat, "Trở về Trang quản lý thi đua");
                _daKhoiTaoToolTip = true;
            }
            catch (Exception ex) { Debug.WriteLine("[InitToolTips] " + ex.Message); }
        }

        // ===================== THOÁT TRANG =====================
        private void kryptonButton1_Thoat_Click(object? sender, EventArgs e)
        {
            // 1. Tìm Form cha (Form2_FormCha)
            var formCha = Application.OpenForms.OfType<Form2_FormCha>().FirstOrDefault();
            if (formCha == null || formCha.IsDisposed) return;
            // 2. Tìm PanelContainer trên Form cha
            if (formCha.Controls.Find("PanelContainer", true).FirstOrDefault() is not Panel panel || panel.IsDisposed) return;
            // 3. Đóng và dọn sạch cái vỏ Form55 khỏi PanelContainer
            var form55 = panel.Controls.OfType<Form55_QuanLyHeThongThiDuaBaNhat>().FirstOrDefault();
            if (form55 != null)
            {
                panel.Controls.Remove(form55);
                form55.Close();
                form55.Dispose();
            }
            // 4. Tìm Form6_XuLyData và hiển thị lại trên mặt trước
            var form6 = panel.Controls.OfType<Form6_XuLyData>().FirstOrDefault();
            if (form6 != null && !form6.IsDisposed)
            {
                form6.Dock = DockStyle.Fill;
                form6.Show();
                form6.BringToFront();
            }
            // 5. Cập nhật lại tiêu đề hệ thống
            formCha.CapNhatTieuDe("Trang phân loại thi đua");
            if (formCha.Label1 != null) formCha.Label1.Text = "Trang phân loại thi đua";
        }

        // ===================== LƯU =====================
        private static int GhiMotDong(string bang, int id, string[] giaTri)
        {
            using var cn = MoKetNoi(false);
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $"UPDATE [{bang}] SET {string.Join(", ", CotSua.Select((c, i) => $"{c} = @p{i}"))} WHERE ID = @id";
            for (int i = 0; i < CotSua.Length; i++)
                cmd.Parameters.AddWithValue($"@p{i}", Module_BaoMatAES.MaHoa(giaTri[i]));
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery();
        }

        private async void kryptonButton_LuuDataSoVang_Click(object? sender, EventArgs e)
        {
            if (!LayIdDangChon(out int id))
            {
                MessageBox.Show("Vui lòng chọn một cán bộ trong danh sách trước khi lưu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int soKyTu = richTextBox1_ThanhTich.TextLength;
            if (soKyTu > Module_BaNhat.GioiHanToiDa)
            {
                MessageBox.Show($"Nội dung thành tích hiện có {soKyTu} ký tự,\nvượt quá giới hạn cho phép ({Module_BaNhat.GioiHanToiDa} ký tự).\nVui lòng rút gọn nội dung trước khi lưu.",
                    "Không thể lưu dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                richTextBox1_ThanhTich.Focus();
                return;
            }

            var nut = kryptonButton_LuuDataSoVang;
            string tenGoc = nut.Text;
            nut.Enabled = false;
            nut.Text = "Đang lưu...";
            try
            {
                string bang = TenBangHienTai;
                // Thứ tự khớp CotSua
                string[] giaTri =
                {
                    kryptonTextBox1_HoVaTen.Text.Trim(), kryptonTextBox1_NamSinh.Text.Trim(), kryptonTextBox1_QueQuan.Text.Trim(),
                    kryptonTextBox1_NgayVaoCAND.Text.Trim(), kryptonTextBox1_CapBac.Text.Trim(), kryptonTextBox1_ChucVu.Text.Trim(),
                    kryptonTextBox1_DonVi.Text.Trim(), kryptonTextBox1_GhiChu.Text.Trim(), richTextBox1_ThanhTich.Text.Trim(),
                    kryptonTextBox1_ThongBaoTrungDoan.Text.Trim(), kryptonTextBox1_SoTTTrongSo.Text.Trim(), kryptonTextBox1_ThangCongNhan.Text.Trim()
                };

                int soDong = await Task.Run(() => GhiMotDong(bang, id, giaTri));   // mã hóa + ghi ở luồng nền
                if (soDong == 0)
                {
                    MessageBox.Show("Không tìm thấy cán bộ cần cập nhật trong Sổ vàng (có thể đã bị xóa).", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Cập nhật thẳng dòng nguồn của lưới (không nạp + giải mã lại cả bảng).
                // Không cần CapNhatTinhTrangSoVangAsync: Số hiệu không bị ghi lại nên tình trạng "Đã đề nghị" không thể đổi.
                string donViCu = "";
                if (kryptonDataGridView1.DataSource is DataTable dt && TimDong(dt, id) is DataRow dong)
                {
                    donViCu = dong["DonVi"] as string ?? "";
                    dong.BeginEdit();
                    for (int i = 0; i < CotSua.Length; i++) dong[CotSua[i]] = giaTri[i];
                    dong.EndEdit();
                }
                if (!string.Equals(donViCu, kryptonTextBox1_DonVi.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                    CapNhatDanhSachDonVi();
                LocDuLieu();

                ThongBaoTam($"Đã lưu thông tin {Module_HeThong.Tu_dong_chi} {giaTri[0]} thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu dữ liệu: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                nut.Enabled = true;
                nut.Text = tenGoc;
            }
        }

        // ===================== XÓA 1 NGƯỜI =====================
        // 1 kết nối, 1 transaction, 1 lệnh UPDATE dùng lại cho mọi dòng (bản cũ tạo N SqliteCommand).
        // Dòng đệm ID = -1 (nếu có) không bị đánh lại STT.
        private static void XoaVaDanhLaiSTT(string bang, int idXoa)
        {
            using var cn = MoKetNoi(false);
            using var tx = cn.BeginTransaction();

            using (var del = cn.CreateCommand())
            {
                del.Transaction = tx;
                del.CommandText = $"DELETE FROM [{bang}] WHERE ID = @id";
                del.Parameters.AddWithValue("@id", idXoa);
                del.ExecuteNonQuery();
            }

            var danhSachId = new List<int>();
            using (var sel = cn.CreateCommand())
            {
                sel.Transaction = tx;
                sel.CommandText = $"SELECT ID FROM [{bang}] WHERE ID <> -1 ORDER BY STT ASC, ID ASC";
                using var rd = sel.ExecuteReader();
                while (rd.Read()) danhSachId.Add(rd.GetInt32(0));
            }

            using (var upd = cn.CreateCommand())
            {
                upd.Transaction = tx;
                upd.CommandText = $"UPDATE [{bang}] SET STT = @stt WHERE ID = @id";
                var pStt = upd.Parameters.Add("@stt", SqliteType.Integer);
                var pId = upd.Parameters.Add("@id", SqliteType.Integer);
                for (int i = 0; i < danhSachId.Count; i++)
                {
                    pStt.Value = i + 1;
                    pId.Value = danhSachId[i];
                    upd.ExecuteNonQuery();
                }
            }
            tx.Commit();
        }

        private async void kryptonButton1_XoaCBCS_Click(object? sender, EventArgs e)
        {
            if (!LayIdDangChon(out int idXoa))
            {
                MessageBox.Show("Vui lòng chọn một cán bộ trong danh sách để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string hoTen = kryptonTextBox1_HoVaTen.Text.Trim();
            string msg = $"Bạn có thực sự muốn xóa {Module_HeThong.Tu_dong_chi}:\n\n" +
                         $"Họ và tên: {hoTen}\n" +
                         $"Số hiệu: {kryptonTextBox1_SoHieu.Text.Trim()}\n" +
                         $"Đơn vị: {kryptonTextBox1_DonVi.Text.Trim()}\n\n" +
                         "Hành động này không thể hoàn tác!";
            if (MessageBox.Show(msg, "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            try
            {
                string bang = TenBangHienTai;
                await Task.Run(() => XoaVaDanhLaiSTT(bang, idXoa));   // lỗi giữa chừng => transaction tự rollback

                // Gỡ dòng + đánh lại STT hiển thị ngay trên bộ nhớ, không nạp + giải mã lại cả bảng
                if (kryptonDataGridView1.DataSource is DataTable dt)
                {
                    var g = kryptonDataGridView1;
                    TatVe(g);
                    try
                    {
                        if (TimDong(dt, idXoa) is DataRow dong) dt.Rows.Remove(dong);
                        int stt = 1;
                        foreach (DataRow r in dt.Rows) r["STT"] = stt++;
                    }
                    finally { BatVe(g); }
                }
                ClearTextBoxes();
                LamMoiBoLocSauKhiDoiDuLieu();
                ThongBaoTam($"Đã xóa {Module_HeThong.Tu_dong_chi} {hoTen} thành công!");

                // Người bị xóa khỏi Sổ vàng => cột "Tình trạng" bên Form42 phải về trống
                await Module_BaNhat.CapNhatTinhTrangSoVangAsync();
                BaoForm55CanNapLai();
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, $"Xóa và xếp lại STT bảng {bang}: {hoTen}", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi thực hiện: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ===================== LÀM MỚI =====================
        private async void kryptonButton_RefershCSDL_Click(object? sender, EventArgs e)
        {
            var nut = kryptonButton_RefershCSDL;
            if (!nut.Enabled) return;     // chặn bấm liên tục
            string tenGoc = nut.Text;
            nut.Enabled = false;
            nut.Text = "Đang tải...";
            try
            {
                DatLaiBoLoc();                              // ô tìm kiếm + đơn vị về mặc định, lọc đúng 1 lần
                bool ok = await NapLuoiAsync();             // nạp lại từ CSDL (đọc cả phiên bản phần mềm)
                ClearTextBoxes();                           // tránh hiển thị dữ liệu cũ
                if (ok) ThongBaoTam("Đã làm mới dữ liệu từ cơ sở dữ liệu.", 1000);
                else CapNhatThongKeSoLuong();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi làm mới dữ liệu: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                nut.Enabled = true;
                nut.Text = tenGoc;
            }
        }

        private void lamMoiHeThong_Click(object? sender, EventArgs e) => kryptonButton_RefershCSDL.PerformClick();
        private void xoaTimKiem_Click(object? sender, EventArgs e) => kryptonButton_LamMoiCacOTimKiem.PerformClick();
        private void toolStripMenuItem_ThoatTrang_Click(object? sender, EventArgs e) => kryptonButton1_Thoat.PerformClick();

        // ===================== XÓA TOÀN BỘ =====================
        private static int DemSoDong(string bang)
        {
            using var cn = MoKetNoi(true);
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM [{bang}] WHERE ID <> -1";
            return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        // Xóa + reset bộ đếm ID trong 1 transaction. Không còn VACUUM: VACUUM viết lại cả tệp .db và khóa độc quyền
        // (làm đứng giao diện nhiều giây), trong khi xóa vài nghìn dòng chữ ngắn không đáng kể về dung lượng.
        private static int XoaTatCa(string bang)
        {
            using var cn = MoKetNoi(false);
            using var tx = cn.BeginTransaction();
            int soDong;
            using (var cmd = cn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = $"DELETE FROM [{bang}]";
                soDong = cmd.ExecuteNonQuery();
            }
            try
            {
                using var cmd = cn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "DELETE FROM sqlite_sequence WHERE name = @n";
                cmd.Parameters.AddWithValue("@n", bang);
                cmd.ExecuteNonQuery();
            }
            catch { /* chưa có sqlite_sequence (bảng chưa từng sinh ID tự tăng) -> bỏ qua, không làm hỏng lệnh xóa */ }
            tx.Commit();
            return soDong;
        }

        private async void toolStripMenuItem_XoaChonTatCa_Click(object? sender, EventArgs e)
        {
            string db = DuongDanDb;
            if (string.IsNullOrWhiteSpace(db) || !File.Exists(db))
            {
                MessageBox.Show("Không tìm thấy tệp cơ sở dữ liệu!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                string bang = TenBangHienTai;
                int soDongTrongBang = await Task.Run(() => DemSoDong(bang));
                if (soDongTrongBang == 0)
                {
                    MessageBox.Show($"Danh sách Sổ vàng hệ thống [{bang}] hiện đang trống.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                DialogResult kq;
                using (var frm = new Form24_XacMinhAdmin())
                {
                    frm.TopMost = true;
                    frm.StartPosition = FormStartPosition.CenterScreen;
                    kq = frm.ShowDialog();
                }
                if (kq != DialogResult.OK) return;

                int soDongDaXoa = await Task.Run(() => XoaTatCa(bang));

                Module_NhatKy.GhiNhatKy(
                    Module_TaiKhoan.TenTaiKhoan_RAM,
                    $"Xóa toàn bộ dữ liệu Sổ vàng Ba Nhất bảng [{bang}] ({soDongDaXoa} dòng)",
                    DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
                MessageBox.Show($"Đã xóa thành công {soDongDaXoa} dòng dữ liệu.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ClearTextBoxes();
                await NapLuoiAsync();                                  // nạp lại danh sách trống (đồng thời cập nhật thống kê)
                await Module_BaNhat.CapNhatTinhTrangSoVangAsync();     // không còn ai trong Sổ vàng => xóa "Đã đề nghị" bên Form42
                BaoForm55CanNapLai();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa dữ liệu:\n{ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ===================== XUẤT / NHẬP EXCEL =====================
        private static string LamSachTenTep(string s)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s;
        }

        private static (string tieuDoan, string trungDoan) DocKyHieuDonVi()
        {
            string tieuDoan = "D2", trungDoan = "E08";    // giá trị mặc định khi CSDL chưa có ký hiệu
            using var cn = MoKetNoi(true);
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "SELECT KyHieu_TieuDoan, KyHieu_TrungDoan FROM KyHieu_DonVi WHERE ID = 1";
            using var rd = cmd.ExecuteReader();
            if (rd.Read())
            {
                string td = SafeGiaiMa(rd.IsDBNull(0) ? null : rd.GetString(0));
                string e = SafeGiaiMa(rd.IsDBNull(1) ? null : rd.GetString(1));
                if (!string.IsNullOrWhiteSpace(td)) tieuDoan = td;
                if (!string.IsNullOrWhiteSpace(e)) trungDoan = e;
            }
            return (tieuDoan, trungDoan);
        }

        private async void ToolStripMenuItem_XuatDanhSach_Click(object? sender, EventArgs e)
        {
            string textBanDau = ToolStripMenuItem_XuatDanhSach.Text;
            Image? anhBanDau = ToolStripMenuItem_XuatDanhSach.Image;
            try
            {
                // 1. Ký hiệu đơn vị để đặt tên tệp (đọc ở luồng nền)
                var (kyHieuTieuDoan, kyHieuTrungDoan) = await Task.Run(() => DocKyHieuDonVi());

                // 2. Chọn nơi lưu
                using var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = LamSachTenTep($"SỔ VÀNG BA NHẤT CỦA {kyHieuTieuDoan} {DateTime.Now:yyyyMMdd_HHmm}") + ".xlsx",
                    Title = "Chọn nơi lưu danh sách Sổ Vàng"
                };
                if (saveFileDialog.ShowDialog() != DialogResult.OK) return;
                string filePath = saveFileDialog.FileName;

                // 3. Khóa nút và gọi module (module tự lo màn hình chờ + mở tệp)
                ToolStripMenuItem_XuatDanhSach.Enabled = false;
                ToolStripMenuItem_XuatDanhSach.Text = "Đang xử lý...";
                ToolStripMenuItem_XuatDanhSach.Image = null;

                await Module_BaNhat.XuatDanhSachSoVangToExcelAsync(this, filePath, TenBangHienTai, kyHieuTrungDoan);

                // Module tự báo lỗi nếu có; chỉ ghi nhật ký "thành công" khi tệp thật sự được tạo
                if (File.Exists(filePath))
                {
                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, $"Đã xuất file sổ vàng thành công tại: {filePath}", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
                    ThongBaoTam($"Đã xuất file sổ vàng thành công tại: {filePath}", 2000);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi nút xuất sổ vàng: {ex.Message}");
                MessageBox.Show("Lỗi khi xuất file Sổ vàng: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ToolStripMenuItem_XuatDanhSach.Text = textBanDau;
                ToolStripMenuItem_XuatDanhSach.Image = anhBanDau;
                ToolStripMenuItem_XuatDanhSach.Enabled = true;
            }
        }

        private async void toolStripMenuItem4_NhapDataVaoCSDL_Click(object? sender, EventArgs e)
        {
            try
            {
                DialogResult kq;
                using (var frm = new Form24_XacMinhAdmin())
                {
                    frm.TopMost = true;
                    frm.StartPosition = FormStartPosition.CenterScreen;
                    kq = frm.ShowDialog(this);
                }
                if (kq != DialogResult.OK) return;

                using var ofd = new OpenFileDialog
                {
                    Title = "Chọn tệp Excel Sổ Vàng để nạp (Yêu cầu cấu trúc 15 cột)",
                    Filter = "Excel Files (*.xlsx, *.xlsm)|*.xlsx;*.xlsm",
                    Multiselect = false,
                    CheckFileExists = true,
                    CheckPathExists = true
                };
                if (ofd.ShowDialog() != DialogResult.OK) return;

                int soDongThanhCong = await Module_BaNhat.NhapDuLieuVaoSoVangTuExcel(this, ofd.FileName, DuongDanDb);
                if (soDongThanhCong > 0)
                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, $"Nạp Excel Sổ Vàng bảng {TenBangHienTai} ({soDongThanhCong} dòng)", DateTime.Now.ToString());

                await NapLuoiAsync();                                  // đã tự dựng lại đơn vị + bộ lọc + thống kê
                if (soDongThanhCong > 0)
                    ThongBaoTam($"Nhập thành công: {soDongThanhCong} {Module_HeThong.Tu_dong_chi} vào Sổ Vàng hệ thống!", 2500);
                await Module_BaNhat.CapNhatTinhTrangSoVangAsync();
                if (soDongThanhCong > 0) BaoForm55CanNapLai();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi nhập dữ liệu: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void toolStripMenuItem_XuatSoVangGoc_Click(object? sender, EventArgs e)
        {
            try
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = $"DanhSach_Goc_SoVangBaNhat_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    Title = "Chọn nơi lưu file Danh sách gốc Sổ Vàng"
                };
                if (sfd.ShowDialog() != DialogResult.OK) return;

                // Hàm trong Module đã tự lo màn hình chờ và mở tệp nên form rất gọn
                await Module_BaNhat.XuatDuLieuGocSoVangRaExcelAsync(this, sfd.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xuất file Sổ vàng gốc: " + ex.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
