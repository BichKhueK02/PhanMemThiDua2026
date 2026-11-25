using DocumentFormat.OpenXml.Bibliography;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using static PhanMemThiDua2026.Module_IconLuoi;
namespace PhanMemThiDua2026
{
    /// <summary>
    /// SỬA / XEM KẾT QUẢ THI ĐUA NHÓM TUẦN (Tân binh + Học viên) - một form dùng chung.
    /// Lưới cố định 6 cột (Tháng xét | Tuần 1..4 | Kết quả tháng); mỗi đợt của Module_LichXet là 1 hàng => đổi lịch
    /// (Form63) chỉ đổi số hàng / nhãn, không sửa form. Ô chọn là DataGridViewComboBoxColumn hiển thị kiểu "Nothing":
    /// không viền, không mũi tên, chỉ hiện hộp chọn khi bấm vào ô.
    /// Được Module_HoSoThiDua giữ trong RAM (đóng = ẩn) để mở tức thì.
    /// </summary>
    public partial class Form62_ChinhSuaDataNhomTuan : Form, IHoSoDungLai
    {
        public bool DungLai { get; set; }
        public bool DangDungSan { get; set; }
        protected override bool ShowWithoutActivation => DangDungSan || base.ShowWithoutActivation;

        private static readonly string[] GiaTri = { "", Module_HeThong.Loai_1, Module_HeThong.Loai_2, Module_HeThong.Loai_3, Module_HeThong.Loai_4 };
        private CheDoDoiTuong _cheDo = CheDoDoiTuong.TanBinh;
        private string _chuKyLich = "";
        private int _id = -1;
        private string _soHieu = "", _hoTen = "", _donVi = "";
        private bool _chiXem;
        private Font? _fontKetQua;
        private readonly ToolTip _goiY = new();
        private string _hoTenDayDu = "", _donViDayDu = "";
        private readonly Dictionary<Control, Font> _fontGoc = new();
        private string TenDoiTuong => _cheDo == CheDoDoiTuong.HocVien ? "học viên" : "tân binh";
        private readonly int _chieuRongGoc = 1121;
        private readonly int _chieuCaoGoc = 401;

        public Form62_ChinhSuaDataNhomTuan()
        {
            InitializeComponent();
            Module_GiaoDienForm62Web.ApDung(this);
            Module_BieuTuongNut.ApDung(this);   // biểu tượng nút tự vẽ
            Icon = Module_BieuTuongNut.IconForm("ButChi");
            // Module_BieuTuongNut vẽ lại ảnh nút lúc Load theo tên control ("CapNhat" => bút chì) => vẽ lại theo chữ hiện tại
            Load += (_, _) => CapNhatBieuTuong();
            // BẤM 1 LẦN LÀ SỔ DANH SÁCH: vào sửa ô + mở hộp chọn ngay (mặc định DataGridView cần 2 lần bấm)
            dataGridView_LichXet.CellMouseDown += Luoi_CellMouseDown;
            dataGridView_LichXet.EditingControlShowing += Luoi_EditingControlShowing;
            Disposed += (_, _) => _goiY.Dispose();
            // Đo lại khi CHÍNH nhãn đổi cỡ (sau khi dàn trang / co giãn DPI), không phải khi khung cha đổi cỡ (lúc đó nhãn còn cỡ cũ)
            label_HoTen.SizeChanged += (_, _) => VuaKhung(label_HoTen, _hoTenDayDu, 18f, 11f);
            label_DonVi.SizeChanged += (_, _) => VuaKhung(label_DonVi, _donViDayDu, 13f, 9f);
            AcceptButton = kryptonButton_CapNhat;
            // Ép lưới vẽ bằng bộ đệm đôi (đổi màu ô không nháy)
            typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(dataGridView_LichXet, true);
            dataGridView_LichXet.CellFormatting += Luoi_CellFormatting;
            // Ô chọn không viền nhưng vẫn có dấu ▾ ở góc phải để người dùng biết là danh sách chọn
            dataGridView_LichXet.CellPainting += Luoi_CellPainting;
            dataGridView_LichXet.CellMouseEnter += (_, e) =>
                dataGridView_LichXet.Cursor = !_chiXem && e.RowIndex >= 0 && e.ColumnIndex > 0 ? Cursors.Hand : Cursors.Default;
            dataGridView_LichXet.MouseLeave += (_, _) => dataGridView_LichXet.Cursor = Cursors.Default;
            dataGridView_LichXet.CurrentCellDirtyStateChanged += (_, _) =>
            {
                // Chọn xong là ghi giá trị ngay => màu ô đổi tức thì, không chờ rời ô
                if (dataGridView_LichXet.IsCurrentCellDirty) dataGridView_LichXet.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dataGridView_LichXet.DataError += (_, e) => e.ThrowException = false;
            dataGridView_LichXet.CellBeginEdit += (_, e) => { if (_chiXem) e.Cancel = true; };
            Module_LichXet.LichThayDoi += KhiLichThayDoi;
            Disposed += (_, _) => Module_LichXet.LichThayDoi -= KhiLichThayDoi;
        }
        private void KhiLichThayDoi() => _chuKyLich = "";   // lần mở sau dựng lại hàng
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (dataGridView_LichXet.IsCurrentCellInEditMode) dataGridView_LichXet.EndEdit();
            if (Module_HoSoThiDua.XuLyDong(this, e)) return;
            base.OnFormClosing(e);
        }

        // ===================== HÀNG THEO LỊCH =====================
        /// <summary>Dựng lại các hàng nếu chế độ / lịch đổi (lần đầu hoặc sau khi cài lại lịch ở Form63).</summary>
        //internal void DamBaoBoCuc(CheDoDoiTuong cheDo)
        //{
        //    var lich = Module_LichXet.LichXet(cheDo);
        //    string chuKy = cheDo + "|" + string.Join(";", lich.Select(d => $"{d.So}:{d.Thang}/{d.Nam}>{d.ThangKetQua}/{d.NamKetQua}"));
        //    if (chuKy == _chuKyLich) return;
        //    _chuKyLich = chuKy;
        //    _cheDo = cheDo;
        //    var g = dataGridView_LichXet;
        //    g.SuspendLayout();
        //    try
        //    {
        //        g.Rows.Clear();
        //        foreach (var d in lich)
        //        {
        //            string nhan = Module_LichXet.NhanThang(d.Thang, d.Nam);
        //            // Kết quả ghi cho tháng kế tiếp => nêu rõ trên nhãn hàng
        //            if (d.ThangKetQua != d.Thang || d.NamKetQua != d.Nam)
        //                nhan += $" → KQ {Module_LichXet.NhanThang(d.ThangKetQua, d.NamKetQua).ToLowerInvariant()}";
        //            int i = g.Rows.Add(nhan, "", "", "", "", "");
        //            g.Rows[i].Tag = d;
        //        }
        //        // Chiều cao form theo số hàng (1..6 tháng), lưới không cần thanh cuộn
        //        int canCao = g.ColumnHeadersHeight + g.Rows.Cast<DataGridViewRow>().Sum(r => r.Height) + 2;
        //        int lech = canCao - g.Height;
        //        if (lech != 0) ClientSize = new Size(ClientSize.Width, ClientSize.Height + lech);
        //        var dau = lich.FirstOrDefault();
        //        var cuoi = lich.LastOrDefault();
        //        label_ChuThich.Text = dau == null ? "" :
        //            $"Lịch xét {TenDoiTuong}: {lich.Count} tháng, từ {Module_LichXet.NhanThang(dau.Thang, dau.Nam)} " +
        //            $"đến {Module_LichXet.NhanThang(cuoi!.Thang, cuoi.Nam)} - mỗi tháng 4 tuần lấy kết quả xét tháng (đổi trong Cài đặt > Lịch xét).";
        //    }
        //    finally { g.ResumeLayout(); }
        //}


        internal void DamBaoBoCuc(CheDoDoiTuong cheDo)
        {
            var lich = Module_LichXet.LichXet(cheDo);
            string chuKy = cheDo + "|" + string.Join(";", lich.Select(d => $"{d.So}:{d.Thang}/{d.Nam}>{d.ThangKetQua}/{d.NamKetQua}"));
            bool doiCheDoHoacLich = chuKy != _chuKyLich;

            if (doiCheDoHoacLich)
            {
                _chuKyLich = chuKy;
                _cheDo = cheDo;

                var g = dataGridView_LichXet;
                g.SuspendLayout();
                try
                {
                    ApDungTyLeCot(cheDo);

                    g.Rows.Clear();
                    foreach (var d in lich)
                    {
                        string nhan = Module_LichXet.NhanThang(d.Thang, d.Nam);
                        if (d.ThangKetQua != d.Thang || d.NamKetQua != d.Nam)
                            nhan += $" → KQ {Module_LichXet.NhanThang(d.ThangKetQua, d.NamKetQua).ToLowerInvariant()}";
                        int i = g.Rows.Add(nhan, "", "", "", "", "");
                        g.Rows[i].Tag = d;
                    }

                    var dau = lich.FirstOrDefault();
                    var cuoi = lich.LastOrDefault();
                    label_ChuThich.Text = dau == null ? "" :
                        $"Lịch xét {TenDoiTuong}: {lich.Count} tháng, từ {Module_LichXet.NhanThang(dau.Thang, dau.Nam)} " +
                        $"đến {Module_LichXet.NhanThang(cuoi!.Thang, cuoi.Nam)} - mỗi tháng 4 tuần lấy kết quả xét tháng (đổi trong Cài đặt > Lịch xét).";
                }
                finally { g.ResumeLayout(); }
            }

            CanChieuCaoVaViTriForm(lich.Count, doiCheDoHoacLich);
        }

        private void ApDungTyLeCot(CheDoDoiTuong cheDo)
        {
            if (cheDo == CheDoDoiTuong.HocVien)
            {
                Cot_ThangXet.FillWeight = 240F;
                Cot_Tuan1.FillWeight = 78F;
                Cot_Tuan2.FillWeight = 78F;
                Cot_Tuan3.FillWeight = 78F;
                Cot_Tuan4.FillWeight = 78F;
                Cot_KetQua.FillWeight = 115F;
            }
            else
            {
                Cot_ThangXet.FillWeight = 190F;
                Cot_Tuan1.FillWeight = 80F;
                Cot_Tuan2.FillWeight = 80F;
                Cot_Tuan3.FillWeight = 80F;
                Cot_Tuan4.FillWeight = 80F;
                Cot_KetQua.FillWeight = 115F;
            }
        }

        private void CanChieuCaoVaViTriForm(int soHang, bool canCanLai)
        {
            var g = dataGridView_LichXet;
            var vungLamViec = Screen.FromControl(this).WorkingArea;

            int rongMongMuon = _cheDo == CheDoDoiTuong.HocVien
                ? Math.Min(_chieuRongGoc + 140, vungLamViec.Width - 32)
                : Math.Min(_chieuRongGoc, vungLamViec.Width - 32);

            int caoLuoiMongMuon = g.ColumnHeadersHeight + Math.Max(1, soHang) * g.RowTemplate.Height + 2;
            int caoKhungNgoai = ClientSize.Height - g.Height;
            int caoMongMuon = caoKhungNgoai + caoLuoiMongMuon;
            int caoToiDa = vungLamViec.Height - 32;

            if (caoMongMuon > caoToiDa)
            {
                caoMongMuon = caoToiDa;
                g.ScrollBars = ScrollBars.Vertical;
            }
            else
            {
                g.ScrollBars = ScrollBars.None;
            }

            caoMongMuon = Math.Max(caoMongMuon, _chieuCaoGoc);

            bool kichThuocDoi = ClientSize.Width != rongMongMuon || ClientSize.Height != caoMongMuon;
            if (kichThuocDoi)
            {
                ClientSize = new Size(rongMongMuon, caoMongMuon);
            }

            if ((canCanLai || kichThuocDoi) && StartPosition == FormStartPosition.CenterScreen)
            {
                int x = vungLamViec.Left + (vungLamViec.Width - Width) / 2;
                int y = vungLamViec.Top + (vungLamViec.Height - Height) / 2;
                Location = new Point(Math.Max(vungLamViec.Left + 8, x), Math.Max(vungLamViec.Top + 8, y));
            }
        }
        //private void Luoi_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        //{
        //    if (e.ColumnIndex <= 0 || e.RowIndex < 0 || e.CellStyle == null) return;
        //    (Color nen, Color chu) = Convert.ToString(e.Value) switch
        //    {
        //        Module_HeThong.Loai_1 => (Color.ForestGreen, Color.White),
        //        Module_HeThong.Loai_2 => (Color.LightGreen, Color.Black),
        //        Module_HeThong.Loai_3 => (Color.Yellow, Color.Black),
        //        Module_HeThong.Loai_4 => (Color.Red, Color.White),
        //        _ => (Color.White, Color.Black)
        //    };
        //    e.CellStyle.BackColor = nen;
        //    e.CellStyle.ForeColor = chu;
        //    e.CellStyle.SelectionBackColor = ControlPaint.Dark(nen, 0.05f);
        //    e.CellStyle.SelectionForeColor = chu;
        //    if (e.ColumnIndex == Cot_KetQua.Index) e.CellStyle.Font = _fontKetQua ??= new Font(dataGridView_LichXet.DefaultCellStyle.Font ?? Font, FontStyle.Bold);
        //}


        /// <summary>
        /// Đầu trang luôn 1 DÒNG (tên đơn vị có thể rất dài, rớt dòng thì xấu): chữ dài thì thu nhỏ cỡ chữ dần tới mức tối thiểu,
        /// vẫn không vừa thì cắt bớt + "…". Rê chuột lên nhãn để xem đầy đủ.
        /// </summary>
        private void VuaKhungDauTrang()
        {
            VuaKhung(label_HoTen, _hoTenDayDu, 18f, 11f);
            VuaKhung(label_DonVi, _donViDayDu, 13f, 9f);
        }
        // Font thu nhỏ dùng chung, KHÔNG hủy (tối đa ~30 cỡ): hủy font khi nhãn còn giữ => nhãn vẽ ra dấu X đỏ
        private static readonly Dictionary<(string Ten, float Co, FontStyle Kieu), Font> _boDemFont = new();
        private static Font LayFont(Font goc, float co)
        {
            if (Math.Abs(co - goc.SizeInPoints) < 0.01f) return goc;
            var khoa = (goc.FontFamily.Name, co, goc.Style);
            lock (_boDemFont)
            {
                if (!_boDemFont.TryGetValue(khoa, out var f)) _boDemFont[khoa] = f = new Font(goc.FontFamily.Name, co, goc.Style);
                return f;
            }
        }
        private void VuaKhung(NhanMotDong lb, string chu, float coGoc, float coMin)
        {
            if (!_fontGoc.TryGetValue(lb, out var goc)) _fontGoc[lb] = goc = lb.Font;   // font của Designer
            Font f = goc;
            if (lb.VungChu.Width > 0)
            {
                float co = goc.SizeInPoints;
                while (co > coMin && !lb.VuaMotDong(chu, f))
                {
                    co -= 0.5f;
                    f = LayFont(goc, co);
                }
            }
            if (!ReferenceEquals(lb.Font, f)) lb.Font = f;
            lb.Text = chu;   // vẫn dài quá ở cỡ nhỏ nhất => NhanMotDong tự cắt "…"
            _goiY.SetToolTip(lb, lb.VuaMotDong(chu, f) ? "" : chu);
        }

        /// <summary>Chế độ xem (Form6): biểu tượng hồ sơ + nút Đóng (dấu X); chế độ sửa (Form15): bút chì + nút Cập nhật.</summary>
        private void CapNhatBieuTuong()
        {
            Icon = Module_BieuTuongNut.IconForm(_chiXem ? "NguoiDung" : "ButChi");
            Module_BieuTuongNut.VeLaiTheoChu(kryptonButton_CapNhat);
        }
        private void Luoi_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (_chiXem || e.Button != MouseButtons.Left || e.RowIndex < 0 || e.ColumnIndex <= 0) return;
            var g = dataGridView_LichXet;
            var o = g.Rows[e.RowIndex].Cells[e.ColumnIndex];
            if (o is not DataGridViewComboBoxCell) return;
            // Đặt ô hiện tại + vào sửa ngay trong lần bấm đầu; mở danh sách sau khi DataGridView xử lý xong cú bấm
            if (g.CurrentCell != o) g.CurrentCell = o;
            if (!g.IsCurrentCellInEditMode) g.BeginEdit(false);
            BeginInvoke(new Action(() =>
            {
                if (g.EditingControl is ComboBox cb && !cb.DroppedDown) cb.DroppedDown = true;
            }));
        }
        private void Luoi_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is not ComboBox cb) return;
            // Chọn xong (bằng chuột hoặc phím Enter) là rời ô => ghi giá trị + tô màu ngay, sẵn sàng bấm ô kế tiếp
            cb.SelectionChangeCommitted -= Combo_DaChon;
            cb.SelectionChangeCommitted += Combo_DaChon;
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            cb.FlatStyle = FlatStyle.Flat;
        }
        private void Combo_DaChon(object? sender, EventArgs e)
        {
            var g = dataGridView_LichXet;
            BeginInvoke(new Action(() =>
            {
                if (g.IsCurrentCellInEditMode) g.EndEdit();
                g.Invalidate();
            }));
        }

        private void Luoi_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex <= 0 || e.RowIndex < 0 || e.CellStyle == null) return;

            string giaTri = Convert.ToString(e.Value) ?? "";
            Color mauChu = giaTri switch
            {
                Module_HeThong.Loai_1 => Color.ForestGreen,
                Module_HeThong.Loai_2 => Color.FromArgb(21, 128, 61),   // xanh lá đậm rõ trên nền trắng
                Module_HeThong.Loai_3 => Color.FromArgb(180, 120, 0),   // vàng cam đậm dễ đọc trên nền trắng
                Module_HeThong.Loai_4 => Color.Crimson,
                _ => Color.FromArgb(55, 65, 81)
            };

            e.CellStyle.BackColor = Color.White;
            e.CellStyle.ForeColor = mauChu;
            e.CellStyle.SelectionBackColor = Color.FromArgb(232, 244, 253);
            e.CellStyle.SelectionForeColor = mauChu;

            bool laKetQua = e.ColumnIndex == Cot_KetQua.Index;
            bool coXepLoai = giaTri is Module_HeThong.Loai_1 or Module_HeThong.Loai_2 or Module_HeThong.Loai_3 or Module_HeThong.Loai_4;
            if (laKetQua || coXepLoai)
            {
                e.CellStyle.Font = _fontKetQua ??= new Font(dataGridView_LichXet.DefaultCellStyle.Font ?? Font, FontStyle.Bold);
            }
        }

        /// <summary>Vẽ ô bình thường rồi thêm dấu ▾ nhỏ ở bên phải (ô chọn, khi được sửa, không đang mở hộp chọn).</summary>
        private void Luoi_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (_chiXem || e.RowIndex < 0 || e.ColumnIndex <= 0 || e.Graphics == null) return;
            var g = dataGridView_LichXet;
            if (g.IsCurrentCellInEditMode && g.CurrentCell?.RowIndex == e.RowIndex && g.CurrentCell.ColumnIndex == e.ColumnIndex) return;

            e.Paint(e.CellBounds, DataGridViewPaintParts.All);

            float k = g.DeviceDpi / 96f;
            float w = 8f * k, h = 4.5f * k;
            float x = e.CellBounds.Right - 10f * k - w, y = e.CellBounds.Top + (e.CellBounds.Height - h) / 2f;
            Color mau = Color.FromArgb(140, 90, 100, 115);

            var cu = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var but = new Pen(mau, 1.6f * k) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round })
                e.Graphics.DrawLines(but, new[] { new PointF(x, y), new PointF(x + w / 2f, y + h), new PointF(x + w, y) });
            e.Graphics.SmoothingMode = cu;
            e.Handled = true;
        }
         //  Nếu bạn muốn tô thêm chấm tròn màu nhỏ trước chữ(ví dụ `● Loại 2`) hay giữ nguyên chữ không chấm thì báo mình chỉnh tiếp nhé.
        /// <summary>Vẽ ô bình thường rồi thêm dấu ▾ nhỏ ở bên phải (ô chọn, khi được sửa, không đang mở hộp chọn).</summary>
        //private void Luoi_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        //{
        //    if (_chiXem || e.RowIndex < 0 || e.ColumnIndex <= 0 || e.Graphics == null) return;
        //    var g = dataGridView_LichXet;
        //    if (g.IsCurrentCellInEditMode && g.CurrentCell?.RowIndex == e.RowIndex && g.CurrentCell.ColumnIndex == e.ColumnIndex) return;
        //    e.Paint(e.CellBounds, DataGridViewPaintParts.All);
        //    float k = g.DeviceDpi / 96f;
        //    float w = 8f * k, h = 4.5f * k;
        //    float x = e.CellBounds.Right - 10f * k - w, y = e.CellBounds.Top + (e.CellBounds.Height - h) / 2f;
        //    Color nen = e.CellStyle?.BackColor ?? Color.White;
        //    // màu mũi tên theo độ sáng nền: nền đậm (Loại 1, Loại 4) => trắng mờ; nền sáng => xám
        //    Color mau = nen.GetBrightness() < 0.45f ? Color.FromArgb(220, 255, 255, 255) : Color.FromArgb(140, 90, 100, 115);
        //    var cu = e.Graphics.SmoothingMode;
        //    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        //    using (var but = new Pen(mau, 1.6f * k) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round })
        //        e.Graphics.DrawLines(but, new[] { new PointF(x, y), new PointF(x + w / 2f, y + h), new PointF(x + w, y) });
        //    e.Graphics.SmoothingMode = cu;
        //    e.Handled = true;
        //}

        // ===================== NẠP / LƯU =====================
        /// <summary>
        /// Nạp hồ sơ (đồng bộ, vài ms) trước khi Show. <paramref name="id"/> &gt; 0: tìm theo ID (Form15 sửa);
        /// ngược lại tìm theo Số hiệu (Form6 xem).
        /// </summary>
        internal void NapHoSo(CheDoDoiTuong cheDo, int id, string soHieu, string hoTen, string donVi, bool chiXem)
        {
            _id = id > 0 ? id : -1;
            _soHieu = soHieu ?? "";
            _hoTen = hoTen ?? "";
            _donVi = donVi ?? "";
            _chiXem = chiXem;
            DialogResult = DialogResult.None;
            SuspendLayout();
            try
            {
                DamBaoBoCuc(cheDo);
                Text = chiXem ? $"Hồ sơ thi đua - {_hoTen}" : $"Thống kê kết quả thi đua của {TenDoiTuong}";
                _hoTenDayDu = _hoTen;
                label_SoHieu.Text = _soHieu;
                _donViDayDu = _donVi;
                VuaKhungDauTrang();
                foreach (DataGridViewRow r in dataGridView_LichXet.Rows)
                    for (int c = 1; c < r.Cells.Count; c++) r.Cells[c].Value = "";
                dataGridView_LichXet.ReadOnly = chiXem;
                Cot_ThangXet.ReadOnly = true;
                label_HuongDan.Visible = !chiXem;
                kryptonButton_CapNhat.Values.Text = chiXem ? "Đóng" : "Cập nhật";
                CapNhatBieuTuong();
                NapDuLieu();
                dataGridView_LichXet.CurrentCell = null;
            }
            finally { ResumeLayout(true); }
        }
        private void NapDuLieu()
        {
            try
            {
                using var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(Module_DanduongGPS.DuongDanCSDL4));
                cn.Open();
                using var cmd = cn.CreateCommand();
                string bang = Module_CheDoDoiTuong.ThiDuaThang(_cheDo);
                if (_id > 0)
                {
                    cmd.CommandText = $"SELECT * FROM [{bang}] WHERE ID = @id";
                    cmd.Parameters.AddWithValue("@id", _id);
                    using var r = cmd.ExecuteReader();
                    if (r.Read()) DoDuLieu(r);
                    return;
                }
                if (string.IsNullOrEmpty(_soHieu)) return;
                cmd.CommandText = $"SELECT * FROM [{bang}]";
                using var r2 = cmd.ExecuteReader();
                int oSoHieu = r2.GetOrdinal("SoHieu");
                while (r2.Read())
                {
                    if (r2.IsDBNull(oSoHieu)) continue;
                    string sh = Form22_ChinhSuaDataCBCS.GiaiMaSafe(r2.GetString(oSoHieu)).Trim();
                    if (string.Equals(sh, _soHieu, StringComparison.OrdinalIgnoreCase))
                    {
                        _id = Convert.ToInt32(r2["ID"]);
                        DoDuLieu(r2);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi load dữ liệu: " + ex.Message, Module_HeThong.TieuDe_Loi, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void DoDuLieu(SqliteDataReader r)
        {
            string hoTen = Form22_ChinhSuaDataCBCS.GiaiMaSafe(r["HoVaTen"]?.ToString());
            if (!string.IsNullOrEmpty(hoTen)) _hoTenDayDu = hoTen;
            string sh = Form22_ChinhSuaDataCBCS.GiaiMaSafe(r["SoHieu"]?.ToString());
            if (!string.IsNullOrEmpty(sh)) label_SoHieu.Text = sh;
            VuaKhungDauTrang();
            var oCot = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < r.FieldCount; i++) oCot[r.GetName(i)] = i;
            foreach (DataGridViewRow hang in dataGridView_LichXet.Rows)
            {
                if (hang.Tag is not DotXet d) continue;
                int c = 1;
                foreach (string ten in d.TatCaCot)
                {
                    string v = oCot.TryGetValue(ten, out int o) && !r.IsDBNull(o) ? Convert.ToString(r.GetValue(o))?.Trim() ?? "" : "";
                    if (v.StartsWith("Loại", StringComparison.OrdinalIgnoreCase)) v = v.Substring(4).Trim();
                    hang.Cells[c++].Value = int.TryParse(v, out int so) && so >= 1 && so <= 4 ? GiaTri[so] : "";
                }
            }
        }
        private void kryptonButton_CapNhat_Click(object? sender, EventArgs e)
        {
            if (_chiXem) { Close(); return; }
            if (_id <= 0)
            {
                MessageBox.Show("Không tìm thấy ID cần cập nhật!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            dataGridView_LichXet.EndEdit();
            try
            {
                using var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(Module_DanduongGPS.DuongDanCSDL4));
                cn.Open();
                using var cmd = cn.CreateCommand();
                var gan = new List<string>();
                int p = 0;
                foreach (DataGridViewRow hang in dataGridView_LichXet.Rows)
                {
                    if (hang.Tag is not DotXet d) continue;
                    int c = 1;
                    foreach (string ten in d.TatCaCot)
                    {
                        int so = Array.IndexOf(GiaTri, Convert.ToString(hang.Cells[c++].Value) ?? "");
                        string ten_p = "@p" + p++;
                        gan.Add($"[{ten}] = {ten_p}");
                        cmd.Parameters.AddWithValue(ten_p, so > 0 ? so : DBNull.Value);
                    }
                }
                if (gan.Count == 0) return;
                cmd.CommandText = $"UPDATE [{Module_CheDoDoiTuong.ThiDuaThang(_cheDo)}] SET {string.Join(", ", gan)} WHERE ID = @id";
                cmd.Parameters.AddWithValue("@id", _id);
                if (cmd.ExecuteNonQuery() > 0)
                {
                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, $"Sửa kết quả thi đua {TenDoiTuong}",
                        $"ID={_id} | {_hoTen} | Số hiệu: {_soHieu}");
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else MessageBox.Show("Không có dữ liệu nào được cập nhật!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Form62] Lưu: " + ex);
                MessageBox.Show("Lỗi lưu dữ liệu thi đua: " + ex.Message, Module_HeThong.TieuDe_Loi, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
