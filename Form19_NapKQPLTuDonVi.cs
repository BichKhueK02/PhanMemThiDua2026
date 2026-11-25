///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.ComponentModel;
using System.Data;
using System.Drawing.Drawing2D;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form19_NapKQPLTuDonVi : Form
    {
        #region Hằng số & trường dữ liệu
        private const string TextSanSang = "Sẵn sàng";
        private const string TextChuaChonTep = "Bấm để chọn tệp Excel (.xlsx, .xlsm)";
        private const string TextNutNhap = "Nhập kết quả";
        private const string GlyphNutNhap = "\uE896";
        /// <summary>Mục đầu combo: nạp mọi dòng của tệp, mỗi dòng theo đúng đơn vị ghi ở dòng đó.</summary>
        internal const string MucTatCaDonVi = "Tất cả đơn vị";
        private const string TextDangXuLy = "Đang xử lý, vui lòng không đóng cửa sổ ...";
        private readonly Form6_XuLyData _form6Ref;
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        /// <summary>Chế độ (CBCS/Tân binh/Học viên) lúc mở form: đổi chế độ thì danh sách đơn vị + bảng đã khác => phải mở form mới.</summary>
        internal CheDoDoiTuong CheDoKhiTao { get; } = Module_CheDoDoiTuong.Hien;
        internal string Csdl2KhiTao => _csdl2Path;
        // --- Chống nạp trùng ---
        private string _donViDaNapThanhCong = "";
        private string _fileDaNapThanhCong = "";
        private string _duongDanFile = "";
        private bool _dangXuLy;
        private readonly System.Windows.Forms.Timer _timerThongBao = new() { Interval = 5000 };
        private readonly List<Font> _ownedFonts = new();
        // --- Controls ---
        private Panel _header = null!;
        private ModernButton _btnHuongDan = null!;
        private ModernButton _btnChonTep = null!;
        private ModernButton _btnNhap = null!;
        private RoundedPanel _pnlFile = null!;
        private Label _lblIconFile = null!;
        private Label _lblDuongDan = null!;
        private ComboBox _cboDonVi = null!;
        private StatusStrip _statusStrip = null!;
        private ToolStripProgressBar _progress = null!;
        private ToolStripStatusLabel _lblStatus = null!;
        private ToolTip _toolTip = null!;
        private Font _fontDuongDan = null!;
        private Font _fontChuaChon = null!;
        private readonly Dictionary<string, string> ColMapVN = new() {
            { "STT", "STT" }, { "Họ và tên", "HoVaTen" }, { "Số hiệu", "SoHieu" },
            { "Năm sinh", "NamSinh" }, { "Quê quán", "QueQuan" }, { "Ngày vào CAND", "NgayVaoCAND" },
            { "Cấp bậc", "CapBac" }, { "Chức vụ", "ChucVu" }, { "Đơn vị", "DonVi" },
            { "Phân loại", "PhanLoai" }, { "Ghi chú", "GhiChu" }
        };
        #endregion
        #region Khởi tạo
        public Form19_NapKQPLTuDonVi(Form6_XuLyData form6)
        {
            components = new Container();
            _form6Ref = form6;
            BuildUi();
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            ShowInTaskbar = false;
            _timerThongBao.Tick += TimerThongBao_Tick;
            Load += Form19_NapKQPLTuDonVi_Load;
            DatDuongDan(null);
            InitToolTips();
        }
        private void Form19_NapKQPLTuDonVi_Load(object? sender, EventArgs e)
        {
            LoadComboBoxDonVi();
        }
        private Font NewFont(string family, float size, FontStyle style = FontStyle.Regular)
        {
            var f = new Font(family, size, style, GraphicsUnit.Point);
            _ownedFonts.Add(f);
            return f;
        }
        private static Image? ResImg(ComponentResourceManager rm, string name)
        {
            try { return rm.GetObject(name) as Image; }
            catch { return null; }
        }
        private void GiaiPhongTaiNguyenTuyChinh()
        {
            _timerThongBao.Stop();
            _timerThongBao.Dispose();
            foreach (var f in _ownedFonts) f.Dispose();
            _ownedFonts.Clear();
        }
        private void InitToolTips()
        {
            _toolTip.IsBalloon = true;
            _toolTip.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
            _toolTip.ToolTipIcon = ToolTipIcon.Info;
            _toolTip.SetToolTip(_btnChonTep, "Chọn tệp dữ liệu kết quả cần nhập vào hệ thống");
            _toolTip.SetToolTip(_cboDonVi, "Chọn đơn vị áp dụng kết quả nhập");
            _toolTip.SetToolTip(_btnNhap, "Thực hiện nhập và đồng bộ kết quả vào cơ sở dữ liệu");
            _toolTip.SetToolTip(_btnHuongDan, "Xem hướng dẫn sử dụng chức năng này");
        }
        #endregion
        #region Dựng giao diện (kiểu web / card)
        private void BuildUi()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(640, 380);
            Font = NewFont(Module_HeThong.TenFontHeThong, 9.75F);
            BackColor = UiColors.Bg;
            Text = "Nhập kết quả phân loại từ đơn vị";
            DoubleBuffered = true;
            var rm = new ComponentResourceManager(typeof(Form19_NapKQPLTuDonVi));
            try
            {
                if (rm.GetObject("$this.Icon") is Icon icon) Icon = icon;
            }
            catch { /* icon không bắt buộc */ }
            var fTitle = NewFont(Module_HeThong.TenFontHeThongDam, 15F);
            var fLabel = NewFont(Module_HeThong.TenFontHeThongDam, 9.5F);
            var fSmall = NewFont(Module_HeThong.TenFontHeThong, 9F);
            var fBtn = NewFont(Module_HeThong.TenFontHeThongDam, 9.75F);
            var fInput = NewFont(Module_HeThong.TenFontHeThong, 10.5F);
            var fIcon = NewFont("Segoe MDL2 Assets", 12F);
            _fontDuongDan = fInput;
            _fontChuaChon = NewFont(Module_HeThong.TenFontHeThong, 10.5F, FontStyle.Italic);
            _toolTip = new ToolTip(components);
            // ===== Status strip =====
            _statusStrip = new StatusStrip { Dock = DockStyle.Bottom, BackColor = Color.White, SizingGrip = false, ImageScalingSize = new Size(20, 20) };
            _progress = new ToolStripProgressBar { Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 30, Size = new Size(90, 16), Visible = false };
            _lblStatus = new ToolStripStatusLabel(TextSanSang) { Spring = true, TextAlign = ContentAlignment.MiddleLeft, Image = ResImg(rm, "toolStripStatusLabel1_ThongBao.Image"), ForeColor = UiColors.Muted };
            _statusStrip.Items.AddRange(new ToolStripItem[] { _progress, _lblStatus });
            // ===== Header =====
            _header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.White };
            _header.Paint += (s, e) =>
            {
                using var p = new Pen(UiColors.Border);
                e.Graphics.DrawLine(p, 0, _header.Height - 1, _header.Width, _header.Height - 1);
            };
            var badge = new RoundedPanel { Location = new Point(24, 15), Size = new Size(42, 42), Radius = 12, FillColor = UiColors.GreenSoft, BorderColor = UiColors.GreenSoft };
            badge.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "\uE898", Font = fIcon, ForeColor = UiColors.GreenDark, TextAlign = ContentAlignment.MiddleCenter });
            var lblTitle = new Label { AutoSize = true, Text = "Nhập kết quả phân loại từ đơn vị", Font = fTitle, ForeColor = UiColors.Text, Location = new Point(78, 10) };
            var lblSub = new Label { AutoSize = true, Text = "Nạp hàng loạt kết quả phân loại thi đua từ tệp Excel vào hệ thống", Font = fSmall, ForeColor = UiColors.Muted, Location = new Point(80, 42) };
            _btnHuongDan = new ModernButton { Text = "Hướng dẫn", Glyph = "\uE897", Font = fBtn, Size = new Size(126, 38), IsPurple = true };
            _btnHuongDan.Click += pictureBox1_Click;
            _header.Controls.AddRange(new Control[] { badge, lblTitle, lblSub, _btnHuongDan });
            _header.Resize += (_, _) => DatViTriNutHeader();
            DatViTriNutHeader();
            // ===== Body =====
            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 20, 12), BackColor = UiColors.Bg };
            var card = new RoundedPanel { Dock = DockStyle.Fill, Padding = new Padding(22, 16, 22, 14) };
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, BackColor = Color.White, Margin = Padding.Empty };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));   // nhãn: tệp Excel
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));   // ô chọn tệp + nút
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));   // nhãn: đơn vị
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));   // danh sách đơn vị
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // đệm
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));   // chân thẻ
            // --- Tệp Excel ---
            tlp.Controls.Add(new Label { Text = "Tệp Excel", Font = fLabel, ForeColor = UiColors.Muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft }, 0, 0);
            var rowFile = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.White, Margin = Padding.Empty };
            rowFile.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rowFile.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
            rowFile.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _pnlFile = new RoundedPanel { Dock = DockStyle.Fill, Radius = 8, FillColor = UiColors.HeaderBack, Margin = new Padding(0, 4, 10, 4), Padding = new Padding(12, 0, 12, 0), Cursor = Cursors.Hand };
            _lblDuongDan = new Label { Dock = DockStyle.Fill, Font = _fontChuaChon, ForeColor = UiColors.Muted, BackColor = UiColors.HeaderBack, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, UseMnemonic = false, Cursor = Cursors.Hand };
            _lblIconFile = new Label { Dock = DockStyle.Left, Width = 30, Text = "\uE8A5", Font = fIcon, ForeColor = UiColors.Muted, BackColor = UiColors.HeaderBack, TextAlign = ContentAlignment.MiddleLeft, Cursor = Cursors.Hand };
            // Thứ tự Add: Fill trước, rồi Left
            _pnlFile.Controls.Add(_lblDuongDan);
            _pnlFile.Controls.Add(_lblIconFile);
            foreach (Control c in new Control[] { _pnlFile, _lblDuongDan, _lblIconFile })
            {
                c.Click += kryptonButton1_ChonDuongDan_Click;
                c.MouseEnter += (_, _) => CapNhatHoverOChonTep();
                c.MouseLeave += (_, _) => CapNhatHoverOChonTep();
            }
            // _btnChonTep = new ModernButton { Text = "Chọn tệp", Glyph = "\uE838", Font = fBtn, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4) };
            _btnChonTep = new ModernButton { Text = "Chọn tệp", Glyph = "\uE838", Font = fBtn, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4), IsPurple = true };
            _btnChonTep.Click += kryptonButton1_ChonDuongDan_Click;
            rowFile.Controls.Add(_pnlFile, 0, 0);
            rowFile.Controls.Add(_btnChonTep, 1, 0);
            tlp.Controls.Add(rowFile, 0, 1);
            // --- Đơn vị ---
            tlp.Controls.Add(new Label { Text = "Đơn vị áp dụng", Font = fLabel, ForeColor = UiColors.Muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft }, 0, 2);
            _cboDonVi = new ComboBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 28,
                IntegralHeight = false,
                MaxDropDownItems = 10,
                Font = fInput,
                Margin = new Padding(0, 4, 0, 4)
            };
            _cboDonVi.DrawItem += CboDonVi_DrawItem;
            tlp.Controls.Add(_cboDonVi, 0, 3);
            // --- Chân thẻ: gợi ý + nút nhập ---
            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.White, Margin = Padding.Empty };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
            footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            footer.Controls.Add(new Label { Text = "Tệp cần có dòng tiêu đề và 3 cột: Số hiệu, Đơn vị, Phân loại.", Font = fSmall, ForeColor = UiColors.Muted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            _btnNhap = new ModernButton { Text = TextNutNhap, Glyph = GlyphNutNhap, Primary = true, Font = fBtn, Dock = DockStyle.Fill, Margin = new Padding(18, 4, 0, 4) };
            _btnNhap.Click += kryptonButton_NhapKetQua_Click;
            footer.Controls.Add(_btnNhap, 1, 0);
            tlp.Controls.Add(footer, 0, 5);
            card.Controls.Add(tlp);
            body.Controls.Add(card);
            // Thứ tự Add: Fill trước, rồi Bottom, rồi Top
            Controls.Add(body);
            Controls.Add(_statusStrip);
            Controls.Add(_header);
            ResumeLayout(false);
            PerformLayout();
        }
        private void DatViTriNutHeader()
        {
            if (_header == null || _btnHuongDan == null) return;
            int top = (_header.Height - _btnHuongDan.Height) / 2;
            _btnHuongDan.Location = new Point(_header.Width - 24 - _btnHuongDan.Width, top);
        }
        /// <summary>Viền ô chọn tệp chuyển xanh khi rê chuột (kiểm tra theo toạ độ thật để không chớp khi chuột đi qua control con).</summary>
        private void CapNhatHoverOChonTep()
        {
            if (_pnlFile == null || _pnlFile.IsDisposed) return;
            bool tren = _pnlFile.RectangleToScreen(_pnlFile.ClientRectangle).Contains(Cursor.Position);
            Color mau = tren && !_dangXuLy ? UiColors.Green : UiColors.Border;
            if (_pnlFile.BorderColor != mau)
            {
                _pnlFile.BorderColor = mau;
                _pnlFile.Invalidate();
            }
        }
        private void CboDonVi_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || sender is not ComboBox cb) return;
            bool dangChon = (e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0;
            Color nen = dangChon ? UiColors.GreenSoft : Color.White;
            Color chu = dangChon ? UiColors.GreenDark : UiColors.Text;
            using (var b = new SolidBrush(nen))
                e.Graphics.FillRectangle(b, e.Bounds);
            TextRenderer.DrawText(e.Graphics, cb.GetItemText(cb.Items[e.Index]), cb.Font,
                new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height), chu,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
        #endregion
        #region Trạng thái & thông báo
        private void HienThongBao(string noiDung, Color mau, bool tuAn = true)
        {
            if (IsDisposed || _lblStatus == null) return;
            _lblStatus.ForeColor = mau;
            _lblStatus.Text = noiDung;
            _timerThongBao.Stop();
            if (tuAn) _timerThongBao.Start();
        }
        private void DatThongBaoMacDinh()
        {
            _lblStatus.ForeColor = UiColors.Muted;
            _lblStatus.Text = TextSanSang;
        }
        private void TimerThongBao_Tick(object? sender, EventArgs e)
        {
            _timerThongBao.Stop();
            if (IsDisposed) return;
            if (_dangXuLy) HienThongBao(TextDangXuLy, UiColors.Info, false);
            else DatThongBaoMacDinh();
        }
        private void DatTrangThaiBan(bool ban)
        {
            _dangXuLy = ban;
            _btnNhap.Enabled = !ban;
            _btnChonTep.Enabled = !ban;
            _cboDonVi.Enabled = !ban;
            _btnNhap.Glyph = ban ? "" : GlyphNutNhap;
            _btnNhap.Text = ban ? "Đang xử lý ..." : TextNutNhap;
            _btnNhap.Invalidate();
            _progress.Visible = ban;
            CapNhatHoverOChonTep();
            if (ban) HienThongBao(TextDangXuLy, UiColors.Info, false);
            else if (_lblStatus.Text == TextDangXuLy) { _timerThongBao.Stop(); DatThongBaoMacDinh(); }
        }
        private void DatDuongDan(string? duongDan)
        {
            _duongDanFile = duongDan?.Trim() ?? "";
            bool coFile = _duongDanFile.Length > 0;
            _lblDuongDan.Text = coFile ? _duongDanFile : TextChuaChonTep;
            _lblDuongDan.Font = coFile ? _fontDuongDan : _fontChuaChon;
            _lblDuongDan.ForeColor = coFile ? UiColors.GreenDark : UiColors.Muted;
            _lblIconFile.ForeColor = coFile ? UiColors.Green : UiColors.Muted;
            _toolTip.SetToolTip(_lblDuongDan, coFile ? _duongDanFile : "Bấm để chọn tệp Excel cần nhập");
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Đang ghi CSDL thì không cho người dùng đóng ngang (đúng như lời nhắc trong phần hướng dẫn)
            if (_dangXuLy && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HienThongBao("Đang nhập dữ liệu, vui lòng đợi hoàn tất rồi hãy đóng cửa sổ.", UiColors.Warn);
                return;
            }
            base.OnFormClosing(e);
        }
        #endregion
        #region Nghiệp vụ nạp kết quả (giữ nguyên logic gốc)
        private void GoiRefershForm6()
        {
            if (_form6Ref != null &&
         !_form6Ref.IsDisposed &&
         _form6Ref.IsHandleCreated)
            {
                _form6Ref.Invoke(new Action(() =>
                {
                    _form6Ref.RefreshCSDL();
                }));
            }
        }
        private DataTable DocExcel(string duongDanFile)
        {
            var dt = new DataTable();
            dt.Columns.Add("SoHieu");
            dt.Columns.Add("DonVi");
            dt.Columns.Add("PhanLoai");
            dt.Columns.Add("HoVaTen");
            dt.Columns.Add("NamSinh");
            dt.Columns.Add("DongExcel", typeof(int));
            try
            {
                using var stream = File.Open(duongDanFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var wb = new XLWorkbook(stream);
                var ws = wb.Worksheets.FirstOrDefault() ?? throw new Exception("File Excel không có Sheet.");
                var headerRow = ws.Row(1) ?? throw new Exception("Không tìm thấy dòng tiêu đề.");
                var colMap = new Dictionary<string, int>();
                int lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
                for (int c = 1; c <= lastCol; c++)
                {
                    string header = headerRow.Cell(c).GetString().Trim();
                    if (!string.IsNullOrEmpty(header) && ColMapVN.TryGetValue(header, out var internalName))
                    {
                        colMap[internalName] = c;
                    }
                }
                string[] requiredCols = { "SoHieu", "DonVi", "PhanLoai" };
                foreach (var col in requiredCols)
                {
                    if (!colMap.ContainsKey(col))
                    {
                        var vnName = ColMapVN.FirstOrDefault(x => x.Value == col).Key;
                        throw new Exception($"File Excel thiếu cột bắt buộc: {vnName}");
                    }
                }
                int oTen = colMap.TryGetValue("HoVaTen", out int ct) ? ct : -1;
                int oNs = colMap.TryGetValue("NamSinh", out int cs) ? cs : -1;
                int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
                for (int r = 2; r <= lastRow; r++)
                {
                    var row = ws.Row(r);
                    if (row == null) continue;
                    string soHieu = BoNhay(row.Cell(colMap["SoHieu"]).GetString());
                    string donVi = row.Cell(colMap["DonVi"]).GetString().Trim();
                    string phanLoai = row.Cell(colMap["PhanLoai"]).GetString().Trim();
                    string hoTen = oTen > 0 ? row.Cell(oTen).GetString().Trim() : "";
                    string namSinh = oNs > 0 ? BoNhay(row.Cell(oNs).GetString()) : "";
                    // Dòng phải có đơn vị và (số hiệu hoặc họ tên). Học viên/Tân binh có thể chưa có số hiệu => tìm theo họ tên.
                    if (donVi.Length == 0 || (soHieu.Length == 0 && hoTen.Length == 0))
                        continue;
                    dt.Rows.Add(soHieu, donVi, phanLoai, hoTen, namSinh, r);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Không đọc được file Excel: " + ex.Message);
            }
            return dt;
        }
        // Tệp xuất ra ghi dấu nháy đơn trước số hiệu/năm sinh để Excel giữ dạng chữ => bỏ nếu còn sót
        private static string BoNhay(string? s) => (s ?? "").Trim().TrimStart('\'').Trim();

        /// <summary>Kết quả 1 lần nạp: đủ để báo rõ cho người dùng VÌ SAO không có dòng nào được cập nhật.</summary>
        private sealed class KetQuaNap
        {
            public int SoDongTep;                              // số dòng hợp lệ trong tệp
            public int SoDongCuaDonVi;                         // số dòng có Đơn vị đúng bằng đơn vị đang chọn
            public int SoCapNhat;
            public int SoDonViDaNap;                           // số đơn vị có ít nhất 1 người được cập nhật
            public List<string> KhongTimThay = new();          // dòng của đơn vị nhưng không tìm được người trong CSDL
            public List<string> DonViTrongTep = new();         // các tên đơn vị có trong tệp (khi không dòng nào khớp đơn vị chọn)
        }

        private KetQuaNap CapNhatPhanLoai(string donViChon, DataTable dtExcel)
        {
            var kq = new KetQuaNap { SoDongTep = dtExcel.Rows.Count };
            var cheDo = Module_CheDoDoiTuong.Hien;
            bool coMaTam = Module_CheDoDoiTuong.LaNhomTuan(cheDo);   // Tân binh / Học viên: số hiệu là mã tạm TB00001 / HV00001
            string ChuanSoHieu(string sh)
            {
                sh = BoNhay(sh);
                return coMaTam && Module_CheDoDoiTuong.LaMaTam(cheDo, sh) ? Module_CheDoDoiTuong.ChuanHoaMaTam(cheDo, sh) : sh;
            }
            bool tatCa = string.Equals(donViChon, MucTatCaDonVi, StringComparison.Ordinal);
            bool ThuocDonViChon(string dv) => tatCa ? dv.Length > 0 : Module_DonVi.CungDonVi(dv, donViChon);
            // Khóa tra cứu luôn kèm tên đơn vị (nguyên văn) => chế độ "Tất cả" không lẫn người trùng tên/số hiệu ở đơn vị khác
            static string Khoa(string dv, string giaTri) => dv + "\u001F" + giaTri;
            static string KhoaTen(string dv, string ten, string namSinh) => Khoa(dv, BoLocDanhSach.ChuanHoaTimKiem(ten.Trim()) + "\u001F" + namSinh.Trim());

            using var cn = new SqliteConnection(Module_KetNoiCSDL.ChuoiKetNoi(_csdl2Path));
            cn.Open();
            using var tran = cn.BeginTransaction();
            try
            {
                // Chỉ xét người của ĐÚNG đơn vị đang chọn — so tên đơn vị nguyên văn (chuỗi chính xác)
                var theoSoHieu = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var theoTenNamSinh = new Dictionary<string, int>(StringComparer.Ordinal);   // -1 = trùng => không dùng
                var theoTen = new Dictionary<string, int>(StringComparer.Ordinal);
                using (var cmdAll = new SqliteCommand(
                    $"SELECT ID, DonVi, SoHieu, HoVaTen, NamSinh FROM {Module_TaiKhoan.BangDanhSach}", cn, tran))
                using (var reader = cmdAll.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            string donViCS = Module_BaoMatAES.GiaiMa(reader["DonVi"]?.ToString() ?? "").Trim();
                            if (!ThuocDonViChon(donViCS)) continue;
                            int id = Convert.ToInt32(reader["ID"]);
                            string soHieuCS = ChuanSoHieu(Module_BaoMatAES.GiaiMa(reader["SoHieu"]?.ToString() ?? ""));
                            string tenCS = Module_BaoMatAES.GiaiMa(reader["HoVaTen"]?.ToString() ?? "").Trim();
                            string nsCS = BoNhay(Module_BaoMatAES.GiaiMa(reader["NamSinh"]?.ToString() ?? ""));
                            if (soHieuCS.Length > 0) theoSoHieu[Khoa(donViCS, soHieuCS)] = id;
                            if (tenCS.Length > 0)
                            {
                                string k1 = KhoaTen(donViCS, tenCS, nsCS), k2 = KhoaTen(donViCS, tenCS, "");
                                theoTenNamSinh[k1] = theoTenNamSinh.ContainsKey(k1) ? -1 : id;
                                theoTen[k2] = theoTen.ContainsKey(k2) ? -1 : id;
                            }
                        }
                        catch
                        {
                            // Bỏ qua bản ghi lỗi giải mã và tiếp tục
                        }
                    }
                }
                using var cmdUpdate = new SqliteCommand(
                    $"UPDATE {Module_TaiKhoan.BangDanhSach} SET PhanLoai=@PhanLoai WHERE ID=@ID", cn, tran);
                var pPhanLoai = cmdUpdate.Parameters.Add("@PhanLoai", SqliteType.Text);
                var pID = cmdUpdate.Parameters.Add("@ID", SqliteType.Integer);
                var donViTep = new List<string>();
                var daGapDonVi = new HashSet<string>(StringComparer.Ordinal);
                var daCapNhat = new HashSet<int>();
                var soDonViDaNap = new HashSet<string>(StringComparer.Ordinal);
                foreach (DataRow excelRow in dtExcel.Rows)
                {
                    string donViExcel = excelRow["DonVi"]?.ToString()?.Trim() ?? "";
                    if (daGapDonVi.Add(donViExcel)) donViTep.Add(donViExcel);
                    if (!ThuocDonViChon(donViExcel)) continue;
                    kq.SoDongCuaDonVi++;
                    string soHieuExcel = ChuanSoHieu(excelRow["SoHieu"]?.ToString() ?? "");
                    string tenExcel = excelRow["HoVaTen"]?.ToString()?.Trim() ?? "";
                    string nsExcel = excelRow["NamSinh"]?.ToString() ?? "";
                    // 1) Số hiệu (CBCS: số hiệu thật; TB/HV: mã tạm). 2) Họ tên + năm sinh. 3) Họ tên (chỉ khi không trùng tên trong đơn vị).
                    int id = -1;
                    if (soHieuExcel.Length > 0 && theoSoHieu.TryGetValue(Khoa(donViExcel, soHieuExcel), out int a)) id = a;
                    else if (tenExcel.Length > 0 && theoTenNamSinh.TryGetValue(KhoaTen(donViExcel, tenExcel, nsExcel), out int b) && b >= 0) id = b;
                    else if (tenExcel.Length > 0 && theoTen.TryGetValue(KhoaTen(donViExcel, tenExcel, ""), out int c) && c >= 0) id = c;
                    if (id < 0)
                    {
                        kq.KhongTimThay.Add($"Dòng {excelRow["DongExcel"]}: {tenExcel} (Số hiệu: {soHieuExcel}){(tatCa ? " - " + donViExcel : "")}");
                        continue;
                    }
                    pPhanLoai.Value = Module_BaoMatAES.MaHoa(excelRow["PhanLoai"]?.ToString()?.Trim() ?? "");
                    pID.Value = id;
                    cmdUpdate.ExecuteNonQuery();
                    if (daCapNhat.Add(id)) kq.SoCapNhat++;
                    soDonViDaNap.Add(donViExcel);
                }
                tran.Commit();
                if (kq.SoDongCuaDonVi == 0) kq.DonViTrongTep = donViTep;
                kq.SoDonViDaNap = tatCa ? soDonViDaNap.Count : (kq.SoCapNhat > 0 ? 1 : 0);
                return kq;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }
        private static string LietKe(List<string> ds, int toiDa = 15)
            => string.Join("\n", ds.Take(toiDa)) + (ds.Count > toiDa ? $"\n... và {ds.Count - toiDa} dòng khác" : "");
        /// <summary>Giải thích vì sao 0 dòng được cập nhật (đơn vị không khớp / không tìm thấy người).</summary>
        private static string MoTaKhongCapNhat(string donViChon, KetQuaNap kq)
        {
            if (kq.SoDongCuaDonVi == 0 && donViChon == MucTatCaDonVi)
                return $"Tệp có {kq.SoDongTep} dòng nhưng không dòng nào ghi Đơn vị.";
            if (kq.SoDongCuaDonVi == 0)
                return $"Tệp có {kq.SoDongTep} dòng nhưng KHÔNG có dòng nào thuộc đơn vị \"{donViChon}\".\n\n"
                     + "Các đơn vị có trong tệp (cột Đơn vị):\n" + LietKe(kq.DonViTrongTep.Select(x => $"\"{x}\"").ToList())
                     + "\n\nTên đơn vị phải ghi giống hệt tên đã khai báo trong phần mềm.";
            return $"Tệp có {kq.SoDongCuaDonVi} dòng thuộc đơn vị \"{donViChon}\" nhưng không tìm thấy người tương ứng trong phần mềm "
                 + "(so theo Số hiệu, rồi Họ tên + Năm sinh):\n\n" + LietKe(kq.KhongTimThay);
        }
        private void LoadComboBoxDonVi()
        {
            try
            {
                _cboDonVi.BeginUpdate();
                _cboDonVi.Items.Clear();
                var danhSachDonVi = Module_DonVi.GetDanhSachDonVi();
                _cboDonVi.Items.Add(MucTatCaDonVi);
                if (danhSachDonVi is { Count: > 0 })
                    _cboDonVi.Items.AddRange(danhSachDonVi.ToArray());
                _cboDonVi.SelectedIndex = 0;
            }
            finally { _cboDonVi.EndUpdate(); }
        }
        #endregion
        #region Sự kiện giao diện
        private void kryptonButton1_ChonDuongDan_Click(object? sender, EventArgs e)
        {
            if (_dangXuLy) return;
            using var ofd = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xlsm",
                Title = "Chọn tệp Excel"
            };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                DatDuongDan(ofd.FileName);
                HienThongBao("Đã chọn tệp excel, vui lòng chọn đơn vị và bấm [Nhập kết quả] để tiến hành đồng bộ.", UiColors.Info);
            }
        }
        private async void kryptonButton_NhapKetQua_Click(object? sender, EventArgs e)
        {
            if (_dangXuLy) return;
            // Lấy thông tin ngay từ đầu để kiểm tra trước
            string donViChon = _cboDonVi.Text.Trim();
            string duongDanFile = _duongDanFile;
            // 1. Kiểm tra rỗng
            if (string.IsNullOrWhiteSpace(donViChon))
            {
                MessageBox.Show(this, "Bạn chưa chọn đơn vị!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(duongDanFile) || !File.Exists(duongDanFile))
            {
                MessageBox.Show(this, "Vui lòng chọn file Excel!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _btnChonTep.PerformClick();
                return;
            }
            // 2. Chốt chặn: bấm lại y xì thông tin vừa nạp thành công
            if (Module_DonVi.CungDonVi(donViChon, _donViDaNapThanhCong) &&
                duongDanFile.Equals(_fileDaNapThanhCong, StringComparison.OrdinalIgnoreCase))
            {
                HienThongBao($"Dữ liệu của \"{donViChon}\" từ tệp này đã nạp rồi. Vui lòng chọn đơn vị/tệp khác!", UiColors.Warn);
                return;
            }
            try
            {
                DatTrangThaiBan(true);
                await Task.Delay(100); // Đợi UI cập nhật trạng thái nút bấm
                // Chạy ngầm tác vụ nạp DB
                KetQuaNap? kqNap = await Task.Run(() =>
                {
                    Module_BaNhat.NhapDuLieuVaoBangQuanLyBaNhat(duongDanFile, _csdl2Path);
                    // Dùng using để thu hồi RAM của DataTable ngay sau khi lấy xong
                    using (DataTable dtExcel = DocExcel(duongDanFile))
                    {
                        if (dtExcel == null || dtExcel.Rows.Count == 0) return null;
                        return CapNhatPhanLoai(donViChon, dtExcel);
                    }
                });
                int soDongCapNhat = kqNap?.SoCapNhat ?? -1;
                // Chặn lỗi văng nếu người dùng lỡ tắt Form lúc đang chạy ngầm
                if (IsDisposed || Disposing)
                    return;
                DatTrangThaiBan(false);
                if (soDongCapNhat == -1)
                {
                    HienThongBao("Tệp Excel không có dữ liệu hợp lệ.", UiColors.Warn);
                    MessageBox.Show(this, "File Excel không có dữ liệu hợp lệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (kqNap != null && soDongCapNhat == 0)
                {
                    HienThongBao("Không có bản ghi nào được cập nhật.", UiColors.Warn);
                    MessageBox.Show(this, MoTaKhongCapNhat(donViChon, kqNap), "Không nạp được kết quả", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (kqNap != null && kqNap.KhongTimThay.Count > 0)
                    MessageBox.Show(this, $"Đã cập nhật {soDongCapNhat} bản ghi. Có {kqNap.KhongTimThay.Count} dòng của đơn vị \"{donViChon}\" không tìm thấy trong phần mềm:\n\n"
                        + LietKe(kqNap.KhongTimThay), "Một số dòng chưa nạp", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Thành công
                string phamVi = donViChon == MucTatCaDonVi ? $"{kqNap?.SoDonViDaNap ?? 0} đơn vị" : $"đơn vị \"{donViChon}\"";
                HienThongBao($"[{DateTime.Now:HH:mm:ss}] Đã cập nhật thành công {soDongCapNhat} bản ghi của {phamVi}.", UiColors.GreenDark);
                Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Nạp kết quả phân loại từ Excel",
                    $"Đơn vị: {donViChon} | Cập nhật {soDongCapNhat} bản ghi | Tệp: {Path.GetFileName(duongDanFile)}");
                // Lưu lịch sử để chống người dùng bấm trùng
                _donViDaNapThanhCong = donViChon;
                _fileDaNapThanhCong = duongDanFile;
                GoiRefershForm6();
                await Module_BieuDoTronTrangChu.CapNhatBieuDoForm4Async();
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                {
                    DatTrangThaiBan(false);
                    HienThongBao("Nhập kết quả không thành công.", UiColors.Danger);
                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Lỗi nạp kết quả phân loại từ Excel", $"Đơn vị: {donViChon} | {ex.Message}");
                    MessageBox.Show(this, $"Có lỗi xảy ra: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (!IsDisposed && _dangXuLy)
                    DatTrangThaiBan(false);
            }
        }
        private void pictureBox1_Click(object? sender, EventArgs e)
        {
            string msgHuongDan =
                "GIỚI THIỆU:\n" +
                "Chức năng này giúp nạp hàng loạt kết quả phân loại thi đua từ file Excel vào hệ thống tự động, bảo mật và chính xác.\n\n" +
                "HƯỚNG DẪN SỬ DỤNG:\n" +
                "1. Chuẩn bị file Excel: Đảm bảo có dòng tiêu đề và chứa 3 cột bắt buộc (viết đúng chính tả): Số hiệu, Đơn vị, Phân loại.\n" +
                "2. Chọn đơn vị: chọn đúng đơn vị cần nạp, hoặc chọn [ Tất cả đơn vị ] để nạp mọi dòng trong tệp một lần (mỗi dòng theo đơn vị ghi ở dòng đó).\n" +
                "   Tên đơn vị trong tệp phải ghi giống hệt tên đã khai báo trong phần mềm. Người được tìm theo Số hiệu, nếu không có thì theo Họ tên + Năm sinh.\n" +
                "3. Chọn tệp: Bấm [ Chọn tệp ] (hoặc bấm vào ô đường dẫn) rồi tìm tệp Excel vừa chuẩn bị.\n" +
                "4. Thực hiện: Bấm [ Nhập kết quả ] và đợi hệ thống xử lý.\n\n" +
                "LƯU Ý:\n" +
                "- Không đóng cửa sổ trong lúc phần mềm đang chạy.\n" +
                "- Hệ thống sẽ tự động đối chiếu, mã hóa dữ liệu và cập nhật biểu đồ sau khi hoàn tất.";
            MessageBox.Show(this, msgHuongDan,
                            "Hướng dẫn sử dụng - Nạp Kết Quả",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
        }
        #endregion
        #region Control tự vẽ (nút, thẻ) & bảng màu
        private static class UiColors
        {
            public static readonly Color Bg = Color.FromArgb(244, 246, 248);
            public static readonly Color Border = Color.FromArgb(229, 231, 235);
            public static readonly Color HeaderBack = Color.FromArgb(249, 250, 251);
            public static readonly Color Hover = Color.FromArgb(243, 244, 246);
            public static readonly Color Text = Color.FromArgb(17, 24, 39);
            public static readonly Color Muted = Color.FromArgb(107, 114, 128);
            public static readonly Color Green = Color.FromArgb(22, 163, 74);
            public static readonly Color GreenHover = Color.FromArgb(21, 128, 61);
            public static readonly Color GreenPressed = Color.FromArgb(20, 83, 45);
            public static readonly Color GreenDark = Color.FromArgb(21, 128, 61);
            public static readonly Color GreenSoft = Color.FromArgb(220, 252, 231);
            public static readonly Color Danger = Color.FromArgb(185, 28, 28);
            public static readonly Color Warn = Color.FromArgb(180, 83, 9);
            public static readonly Color Info = Color.FromArgb(29, 78, 216);
            public static readonly Color Purple = Color.FromArgb(237, 233, 254);
            public static readonly Color PurpleHover = Color.FromArgb(221, 214, 254);
            public static readonly Color PurplePressed = Color.FromArgb(196, 181, 253);
            public static readonly Color PurpleDark = Color.FromArgb(109, 40, 217);
        }
        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = Math.Max(1, radius * 2);
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
        private sealed class RoundedPanel : Panel
        {
            private Color _fill = Color.White;
            public int Radius { get; set; } = 12;
            public Color BorderColor { get; set; } = UiColors.Border;
            public Color FillColor
            {
                get => _fill;
                set { _fill = value; BackColor = value; Invalidate(); }
            }
            public RoundedPanel()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                BackColor = Color.White;
            }
            protected override void OnPaintBackground(PaintEventArgs e) =>
                e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
                using var b = new SolidBrush(_fill);
                using var p = new Pen(BorderColor);
                g.FillPath(b, path);
                g.DrawPath(p, path);
            }
        }
        private sealed class ModernButton : Button
        {
            private static readonly Font IconFont = new("Segoe MDL2 Assets", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
            private bool _hover, _down;
            public string Glyph { get; set; } = "";
            public bool Primary { get; set; }
            public bool IsPurple { get; set; }
            public int Radius { get; set; } = 8;
            public ModernButton()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;
                UseVisualStyleBackColor = false;
                Cursor = Cursors.Hand;
            }
            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
            protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Parent?.BackColor ?? SystemColors.Control);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color fill, border, fore;
                if (Primary)
                {
                    fill = _down ? UiColors.GreenPressed : _hover ? UiColors.GreenHover : UiColors.Green;
                    border = fill;
                    fore = Color.White;
                }
                else if (IsPurple)
                {
                    fill = _down ? UiColors.PurplePressed : _hover ? UiColors.PurpleHover : UiColors.Purple;
                    border = Color.FromArgb(196, 181, 253);
                    fore = UiColors.PurpleDark;
                }
                else
                {
                    fill = _down ? Color.FromArgb(229, 231, 235) : _hover ? UiColors.Hover : Color.White;
                    border = Color.FromArgb(209, 213, 219);
                    fore = UiColors.Text;
                }
                if (!Enabled)
                {
                    fill = Color.FromArgb(243, 244, 246);
                    border = Color.FromArgb(229, 231, 235);
                    fore = Color.FromArgb(156, 163, 175);
                }
                using (var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius))
                using (var b = new SolidBrush(fill))
                using (var p = new Pen(border))
                {
                    g.FillPath(b, path);
                    g.DrawPath(p, path);
                }
                const TextFormatFlags fl = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left;
                var huge = new Size(int.MaxValue, int.MaxValue);
                Size gs = string.IsNullOrEmpty(Glyph) ? Size.Empty : TextRenderer.MeasureText(g, Glyph, IconFont, huge, fl);
                Size ts = string.IsNullOrEmpty(Text) ? Size.Empty : TextRenderer.MeasureText(g, Text, Font, huge, fl);
                int gap = gs.Width > 0 && ts.Width > 0 ? 8 : 0;
                int x = (Width - (gs.Width + gap + ts.Width)) / 2;
                if (gs.Width > 0)
                {
                    TextRenderer.DrawText(g, Glyph, IconFont, new Rectangle(x, 0, gs.Width + 2, Height), fore, fl);
                    x += gs.Width + gap;
                }
                if (ts.Width > 0)
                    TextRenderer.DrawText(g, Text, Font, new Rectangle(x, 0, ts.Width + 2, Height), fore, fl);
            }
        }
        #endregion
    }
}