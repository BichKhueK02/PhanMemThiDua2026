///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using System.Globalization;
using System.Text;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form14 : Form
    {
        private readonly StringBuilder _expression = new();
        private bool _justCalculated;
        // Dòng cuối của ListBox đang là "dòng đang gõ" (được cập nhật tại chỗ khi gõ tiếp)
        private bool _coDongDangNhap;
        // [FIX 5]: Chống spam phím
        private DateTime _lastInputTime = DateTime.MinValue;
        private const int INPUT_DELAY = 40;
        private const int MAX_LENGTH = 100;
        // [PRO 2]: Lặp phép tính khi bấm '=' liên tiếp
        private char _lastOp = '\0';
        private double _lastRightOperand = 0;
        private string _lastResultText = string.Empty;
        public Form14()
        {
            InitializeComponent();
            Module_HeThong.BocBoTronGiaoDien(ListBox1);
            InitializeForm();
        }
        //region ===== INITIALIZE =====
        private void InitializeForm()
        {
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            KeyPress += Form_KeyPress;
            KeyDown += Form_KeyDown;
            // Enter được xử lý trong ProcessCmdKey (không dùng AcceptButton, tránh Enter bấm lại phím đang giữ focus)
            AcceptButton = null;
            // Nút dấu thập phân hiển thị đúng dấu của máy (',' hoặc '.')
            Btn_phimdauphay.Text = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            AttachButtons(this);
            // [CẤU HÌNH CUSTOM HIGHLIGHT CHO LISTBOX1]
            ListBox1.DrawMode = DrawMode.OwnerDrawFixed;
            ListBox1.DrawItem += ListBox1_DrawItem;
        }
        private void ListBox1_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || sender is not ListBox lb) return;
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            string text = lb.Items[e.Index]?.ToString() ?? string.Empty;
            bool laBieuThuc = text.EndsWith(" =", StringComparison.Ordinal);
            bool laLoi = text.StartsWith("Lỗi", StringComparison.Ordinal);
            // Giao diện sáng: dòng chọn nền xanh nhạt, biểu thức xám, kết quả đậm, lỗi đỏ
            Color backColor = isSelected ? Color.FromArgb(219, 234, 254) : lb.BackColor;
            Color textColor = laLoi ? Color.FromArgb(185, 28, 28)
                            : laBieuThuc ? Color.FromArgb(100, 116, 139)
                            : isSelected ? Color.FromArgb(30, 64, 175)
                            : lb.ForeColor;
            using (SolidBrush backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }
            Rectangle r = Rectangle.Inflate(e.Bounds, -10, 0);
            const TextFormatFlags dd = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            float size = laBieuThuc ? 14f : lb.Font.SizeInPoints;
            FontStyle style = laBieuThuc ? FontStyle.Regular : lb.Font.Style;
            Font f = new Font(lb.Font.FontFamily, size, style, GraphicsUnit.Point);
            try
            {
                // Dòng dài: thu nhỏ chữ dần (tối thiểu 10pt), vẫn dài thì cắt phần đầu và thêm "…"
                while (size > 10f &&
                       TextRenderer.MeasureText(e.Graphics, text, f, new Size(int.MaxValue, e.Bounds.Height), dd).Width > r.Width)
                {
                    size -= 1f;
                    f.Dispose();
                    f = new Font(lb.Font.FontFamily, size, style, GraphicsUnit.Point);
                }
                bool cat = false;
                string tail = text;
                while (tail.Length > 1 &&
                       TextRenderer.MeasureText(e.Graphics, (cat ? "…" : "") + tail, f, new Size(int.MaxValue, e.Bounds.Height), dd).Width > r.Width)
                {
                    tail = tail.Substring(1);
                    cat = true;
                }
                TextRenderer.DrawText(e.Graphics, (cat ? "…" : "") + tail, f, r, textColor,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | dd);
            }
            finally
            {
                f.Dispose();
            }
        }
        private void AttachButtons(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                if (ctrl is Button btn)
                {
                    btn.TabStop = false;
                    btn.Click += Button_Click;
                }
                if (ctrl.HasChildren) AttachButtons(ctrl);
            }
        }
        //endregion
        //region ===== INPUT HANDLING =====
        private void Button_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn) return;
            switch (btn.Name)
            {
                case "Btn_phimdaubang": Calculate(); break;
                case "Btn_phimdauxoaall": ClearAllFull(); break;
                case "Btn_phimdauxoa": DeleteLast(); break;
                default: ProcessInput(btn.Text.Trim()); break;
            }
            // Trả focus khỏi nút vừa bấm để Enter / Space không bấm lặp lại chính nút đó
            ActiveControl = null;
        }
        // Enter (kể cả Enter bàn phím số) luôn là dấu '=', bất kể đang focus ở đâu
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                Calculate();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        // Chuẩn hóa 1 ký tự (từ phím, nút hoặc dán) về ký tự biểu thức; null nếu không dùng
        private static string? ChuanHoaPhim(char c)
        {
            if (c >= '0' && c <= '9') return c.ToString();
            switch (c)
            {
                case '+':
                case '-':
                case '*':
                case '/':
                case '(':
                case ')':
                case '%':
                case '^':
                case '√':
                    return c.ToString();
                case 'x': case 'X': case '×': return "*";
                case ':': case '÷': return "/";
                case '−': return "-";
                case '@': return "√";
                case '.':
                case ',':
                    return CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0].ToString();
            }
            return null;
        }
        private void Form_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '=')
            {
                Calculate();
                e.Handled = true;
                return;
            }
            string? v = ChuanHoaPhim(e.KeyChar);
            if (v != null)
            {
                ProcessInput(v);
                e.Handled = true;
            }
        }
        private void Form_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Back)
            {
                DeleteLast();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Escape)
            {
                ClearAll();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                CopyResult();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                PasteExpression();
                e.SuppressKeyPress = true;
            }
        }
        private static bool LaToanTuHai(char c) => c == '+' || c == '-' || c == '*' || c == '/' || c == '^';
        private static bool LaChuSo(char c) => c >= '0' && c <= '9';
        private void ProcessInput(string value)
        {
            // [FIX 5]: Chống spam & Giới hạn độ dài
            if ((DateTime.Now - _lastInputTime).TotalMilliseconds < INPUT_DELAY) return;
            _lastInputTime = DateTime.Now;
            NhapKyTu(value);
        }
        private void NhapKyTu(string value)
        {
            if (_expression.Length >= MAX_LENGTH) return;
            if (string.IsNullOrWhiteSpace(value)) return;
            value = value.Trim();
            if (value.Length != 1) return;
            string? chuan = ChuanHoaPhim(value[0]);
            if (chuan == null) return;
            char ch = chuan[0];
            char dec = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
            // [PRO 1]: Auto thêm ngoặc cho căn bậc 2
            if (ch == '√')
            {
                if (_justCalculated) { _expression.Clear(); _justCalculated = false; }
                _expression.Append("√(");
                RefreshDisplay();
                return;
            }
            if (LaChuSo(ch))
            {
                if (_justCalculated) { _expression.Clear(); _justCalculated = false; }
                // Không cho số 0 dẫn đầu: "0" rồi gõ "5" thành "5"
                if (GetCurrentNumber() == "0") _expression.Length--;
                _expression.Append(ch);
                RefreshDisplay();
                return;
            }
            if (ch == dec)
            {
                if (_justCalculated) { _expression.Clear(); _justCalculated = false; }
                string cur = GetCurrentNumber();
                if (cur.Contains(dec)) return;
                if (cur.Length == 0) _expression.Append('0');
                _expression.Append(dec);
                RefreshDisplay();
                return;
            }
            if (ch == '(')
            {
                if (_justCalculated) { _expression.Clear(); _justCalculated = false; }
                _expression.Append('(');
                RefreshDisplay();
                return;
            }
            if (ch == ')')
            {
                int mo = 0, dong = 0;
                foreach (char c in _expression.ToString()) { if (c == '(') mo++; else if (c == ')') dong++; }
                if (mo <= dong || _expression.Length == 0) return;
                char cuoi = _expression[^1];
                if (!(LaChuSo(cuoi) || cuoi == ')' || cuoi == '%' || cuoi == dec)) return;
                _expression.Append(')');
                RefreshDisplay();
                return;
            }
            if (ch == '%')
            {
                if (_expression.Length == 0) return;
                char cuoi = _expression[^1];
                if (!(LaChuSo(cuoi) || cuoi == ')' || cuoi == '%')) return;
                _justCalculated = false;
                _expression.Append('%');
                RefreshDisplay();
                return;
            }
            // Còn lại: + - * / ^
            _justCalculated = false; // Reset cờ để không bị lặp phép tính nhầm
            if (_expression.Length == 0)
            {
                if (ch == '-') { _expression.Append(ch); RefreshDisplay(); }
                return;
            }
            char last = _expression[^1];
            if (last == '(')
            {
                // Sau '(' chỉ cho dấu âm
                if (ch == '-') { _expression.Append(ch); RefreshDisplay(); }
                return;
            }
            if (LaToanTuHai(last))
            {
                // "5*" rồi gõ '-' => số âm: 5*-3
                if (ch == '-' && (last == '*' || last == '/' || last == '^'))
                {
                    _expression.Append(ch);
                    RefreshDisplay();
                    return;
                }
                // Còn lại: thay toán tử cũ bằng toán tử mới
                while (_expression.Length > 0 && LaToanTuHai(_expression[^1])) _expression.Length--;
                if (_expression.Length == 0)
                {
                    if (ch == '-') _expression.Append(ch);
                    RefreshDisplay();
                    return;
                }
            }
            _expression.Append(ch);
            RefreshDisplay();
        }
        //endregion
        //region ===== CALCULATE =====
        private void Calculate()
        {
            if (_expression.Length == 0) return;
            // Vừa tính xong mà không có phép tính để lặp (ví dụ chỉ có 1 số) => bỏ qua
            if (_justCalculated && _lastOp == '\0') return;
            try
            {
                // [PRO 2]: Lặp phép tính nếu user vừa tính xong và ấn '=' tiếp
                if (_justCalculated && _lastOp != '\0')
                {
                    _expression.Append(_lastOp).Append(DinhDang(_lastRightOperand));
                }
                // Tự đóng các ngoặc còn thiếu: (2+3 => (2+3)
                int mo = 0, dong = 0;
                foreach (char c in _expression.ToString()) { if (c == '(') mo++; else if (c == ')') dong++; }
                for (int i = 0; i < mo - dong; i++) _expression.Append(')');
                string rawExpression = _expression.ToString();
                var boPhanTich = new BoPhanTich(rawExpression, CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0]);
                double result = boPhanTich.TinhToan();
                string ketQua = DinhDang(result);
                // Dòng đang gõ được đổi thành "biểu thức =" (không bị lặp 2 dòng)
                if (_coDongDangNhap && ListBox1.Items.Count > 0)
                    ListBox1.Items[ListBox1.Items.Count - 1] = rawExpression + " =";
                else
                    ListBox1.Items.Add(rawExpression + " =");
                _coDongDangNhap = false;
                ListBox1.Items.Add(ketQua);
                // [PRO 3]: Highlight kết quả và cuộn xuống
                ListBox1.SelectedIndex = ListBox1.Items.Count - 1;
                CuonXuongCuoi();
                // Ghi nhớ phép tính cuối (gốc của biểu thức) để lặp; biểu thức chỉ có 1 số thì không lặp
                _lastOp = boPhanTich.LastOp;
                _lastRightOperand = boPhanTich.LastRight;
                _lastResultText = ketQua;
                _expression.Clear();
                _expression.Append(ketQua);
                _justCalculated = true;
            }
            // [FIX 2]: Phân biệt Exception cực chuẩn
            catch (DivideByZeroException) { ShowError("Lỗi: Không thể chia cho 0"); }
            catch (LoiBieuThuc ex) { ShowError(ex.Message); }
            catch (ArithmeticException ex) { ShowError(ex.Message); }
            catch (Exception) { ShowError("Lỗi: Biểu thức không hợp lệ"); }
        }
        /// <summary>
        /// Định dạng kết quả: làm tròn 15 chữ số có nghĩa (0.1+0.2 = 0.3), không bao giờ ra dạng 1E-05
        /// để chuỗi kết quả luôn đọc lại được thành biểu thức tiếp theo.
        /// </summary>
        private static string DinhDang(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v))
                throw new ArithmeticException("Lỗi: Kết quả vượt giới hạn");
            decimal d;
            try { d = (decimal)v; }
            catch (OverflowException) { throw new ArithmeticException("Lỗi: Kết quả quá lớn"); }
            if (d == 0m) d = 0m; // bỏ dấu âm của -0
            return d.ToString("0.############################", CultureInfo.CurrentCulture);
        }
        private sealed class LoiBieuThuc : Exception
        {
            public LoiBieuThuc(string message) : base(message) { }
        }
        /// <summary>
        /// Bộ phân tích đệ quy: + - (thấp) &lt; * / và nhân ngầm &lt; dấu âm đơn &lt; ^ (kết hợp phải) &lt; % (hậu tố) &lt; số / ( ) / √.
        /// "200+10%" = 200 + 10% của 200 = 220 (giống máy tính Windows); "-2^2" = -4.
        /// </summary>
        private sealed class BoPhanTich
        {
            private readonly string _s;
            private readonly char _dec;
            private int _i;
            private int _depth;
            public char LastOp { get; private set; } = '\0';
            public double LastRight { get; private set; }
            public BoPhanTich(string s, char dec) { _s = s; _dec = dec; }
            public double TinhToan()
            {
                double v = BieuThuc();
                if (_i < _s.Length)
                    throw new LoiBieuThuc(_s[_i] == ')' ? "Lỗi: Thừa dấu ngoặc đóng" : "Lỗi: Biểu thức không hợp lệ");
                return v;
            }
            private void Ghi(char op, double phai)
            {
                if (_depth == 0) { LastOp = op; LastRight = phai; }
            }
            private static void Kiem(double v)
            {
                if (double.IsNaN(v) || double.IsInfinity(v))
                    throw new ArithmeticException("Lỗi: Kết quả vượt giới hạn");
            }
            private bool BatDauToanHang(char c) => LaChuSo(c) || c == _dec || c == '(' || c == '√';
            private double BieuThuc()
            {
                double a = HangTu(out _);
                while (_i < _s.Length && (_s[_i] == '+' || _s[_i] == '-'))
                {
                    char op = _s[_i++];
                    double b = HangTu(out bool laPhanTram);
                    if (laPhanTram) b = a * b; // a + b% = a + (a * b / 100)
                    a = op == '+' ? a + b : a - b;
                    Kiem(a);
                    Ghi(op, b);
                }
                return a;
            }
            private double HangTu(out bool laPhanTram)
            {
                double a = DonVi(out laPhanTram);
                while (_i < _s.Length)
                {
                    char c = _s[_i];
                    if (c == '*' || c == '/')
                    {
                        _i++;
                        double b = DonVi(out _);
                        if (c == '/')
                        {
                            if (b == 0) throw new DivideByZeroException();
                            a /= b;
                        }
                        else a *= b;
                        Kiem(a);
                        Ghi(c, b);
                        laPhanTram = false;
                    }
                    else if (BatDauToanHang(c)) // nhân ngầm: 2(3+4), (1+2)(3+4), 50%3, 2√(9)
                    {
                        double b = DonVi(out _);
                        a *= b;
                        Kiem(a);
                        Ghi('*', b);
                        laPhanTram = false;
                    }
                    else break;
                }
                return a;
            }
            private double DonVi(out bool laPhanTram)
            {
                laPhanTram = false;
                if (_i < _s.Length && (_s[_i] == '-' || _s[_i] == '+'))
                {
                    char dau = _s[_i++];
                    double v = DonVi(out _);
                    return dau == '-' ? -v : v;
                }
                return LuyThua(out laPhanTram);
            }
            private double LuyThua(out bool laPhanTram)
            {
                double a = HauTo(out laPhanTram);
                if (_i < _s.Length && _s[_i] == '^')
                {
                    _i++;
                    double b = DonVi(out _); // kết hợp phải: 2^3^2 = 2^(3^2)
                    a = Math.Pow(a, b);
                    Kiem(a);
                    Ghi('^', b);
                    laPhanTram = false;
                }
                return a;
            }
            private double HauTo(out bool laPhanTram)
            {
                double v = NguyenTo();
                laPhanTram = false;
                while (_i < _s.Length && _s[_i] == '%')
                {
                    _i++;
                    v /= 100;
                    laPhanTram = true;
                }
                return v;
            }
            private double NguyenTo()
            {
                if (_i >= _s.Length) throw new LoiBieuThuc("Lỗi: Biểu thức chưa hoàn chỉnh");
                char c = _s[_i];
                if (c == '(')
                {
                    _i++;
                    _depth++;
                    double v = BieuThuc();
                    if (_i >= _s.Length || _s[_i] != ')') throw new LoiBieuThuc("Lỗi: Thiếu dấu ngoặc đóng");
                    _i++;
                    _depth--;
                    return v;
                }
                if (c == '√')
                {
                    _i++;
                    _depth++;
                    double v = NguyenTo();
                    _depth--;
                    // [FIX 1]: Chặn Sqrt số âm bẻ gãy UX
                    if (v < 0) throw new ArithmeticException("Lỗi: Không thể căn số âm");
                    return Math.Sqrt(v);
                }
                if (LaChuSo(c) || c == _dec)
                {
                    int st = _i;
                    bool coDec = false;
                    while (_i < _s.Length && (LaChuSo(_s[_i]) || _s[_i] == _dec))
                    {
                        if (_s[_i] == _dec)
                        {
                            if (coDec) throw new LoiBieuThuc("Lỗi: Số không hợp lệ");
                            coDec = true;
                        }
                        _i++;
                    }
                    string t = _s.Substring(st, _i - st).Replace(_dec, '.');
                    if (!double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double n))
                        throw new LoiBieuThuc("Lỗi: Số không hợp lệ");
                    return n;
                }
                throw new LoiBieuThuc("Lỗi: Biểu thức không hợp lệ");
            }
        }
        //endregion
        //region ===== UTIL =====
        private string GetCurrentNumber()
        {
            if (_expression.Length == 0) return string.Empty;
            var sb = new StringBuilder();
            for (int i = _expression.Length - 1; i >= 0; i--)
            {
                char c = _expression[i];
                if (LaToanTuHai(c) || c == '(' || c == ')' || c == '%' || c == '√') break;
                sb.Insert(0, c);
            }
            return sb.ToString();
        }
        private void DeleteLast()
        {
            if (_expression.Length == 0) return;
            // "√(" là 1 khối: xóa cả hai ký tự
            if (_expression.Length >= 2 && _expression[^1] == '(' && _expression[^2] == '√')
                _expression.Length -= 2;
            else
                _expression.Length--;
            RefreshDisplay();
        }
        // Xóa dòng đang nhập (Delete / Esc), giữ nguyên lịch sử
        private void ClearAll()
        {
            _expression.Clear();
            _justCalculated = false;
            RefreshDisplay();
        }
        // Nút "Xóa hết": xóa cả lịch sử
        private void ClearAllFull()
        {
            _expression.Clear();
            _justCalculated = false;
            _lastOp = '\0';
            _lastResultText = string.Empty;
            _coDongDangNhap = false;
            ListBox1.Items.Clear();
        }
        // [FIX 4]: RefreshDisplay - dòng đang gõ được cập nhật tại chỗ, không đè lên dòng kết quả cũ
        private void RefreshDisplay()
        {
            string text = _expression.ToString();
            _justCalculated = false;
            if (text.Length == 0)
            {
                if (_coDongDangNhap && ListBox1.Items.Count > 0)
                    ListBox1.Items.RemoveAt(ListBox1.Items.Count - 1);
                _coDongDangNhap = false;
                return;
            }
            if (_coDongDangNhap && ListBox1.Items.Count > 0)
                ListBox1.Items[ListBox1.Items.Count - 1] = text;
            else
                ListBox1.Items.Add(text);
            _coDongDangNhap = true;
            CuonXuongCuoi();
        }
        // Cuộn để dòng cuối nằm ở đáy ô hiển thị (không để trống phía dưới)
        private void CuonXuongCuoi()
        {
            int hienThi = Math.Max(1, ListBox1.ClientSize.Height / Math.Max(1, ListBox1.ItemHeight));
            ListBox1.TopIndex = Math.Max(0, ListBox1.Items.Count - hienThi);
        }
        private void ShowError(string message)
        {
            string expr = _expression.ToString();
            if (_coDongDangNhap && ListBox1.Items.Count > 0)
                ListBox1.Items[ListBox1.Items.Count - 1] = expr;
            else if (expr.Length > 0)
                ListBox1.Items.Add(expr);
            ListBox1.Items.Add(message);
            ListBox1.SelectedIndex = ListBox1.Items.Count - 1;
            CuonXuongCuoi();
            _coDongDangNhap = false;
            _expression.Clear();
            _justCalculated = false;
            _lastOp = '\0'; // Xóa cache lặp phép tính
        }
        // Ctrl+C: chép kết quả gần nhất (nếu chưa có thì chép biểu thức đang gõ)
        private void CopyResult()
        {
            string s = _lastResultText.Length > 0 ? _lastResultText : _expression.ToString();
            if (string.IsNullOrWhiteSpace(s)) return;
            try { Clipboard.SetText(s); }
            catch (Exception) { /* clipboard đang bận: bỏ qua */ }
        }
        // Ctrl+V: dán biểu thức, chỉ nhận các ký tự hợp lệ
        private void PasteExpression()
        {
            string text;
            try { text = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty; }
            catch (Exception) { return; }
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c)) continue;
                NhapKyTu(c.ToString());
            }
        }
        //endregion
    }
}