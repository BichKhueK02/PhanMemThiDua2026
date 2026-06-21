using System.Globalization;
using System.Text;

namespace PhanMemThiDua2026
{
    public partial class Form14 : Form
    {
        private readonly StringBuilder _expression = new();
        private bool _justCalculated;

        // [FIX 5]: Chống spam phím
        private DateTime _lastInputTime = DateTime.MinValue;
        private const int INPUT_DELAY = 40;
        private const int MAX_LENGTH = 100;

        // [PRO 2]: Lặp phép tính
        private char _lastOp = '\0';
        private double _lastRightOperand = 0;

        private readonly Dictionary<char, int> _operators = new()
        {
            { '+', 1 }, { '-', 1 }, { '*', 2 }, { '/', 2 }, { '%', 2 }, { '^', 3 }, { '√', 4 }
        };

        public Form14()
        {
            InitializeComponent();
            InitializeForm();
        }

        #region ===== INITIALIZE =====
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
            AcceptButton = Btn_phimdaubang;
            AttachButtons(this);
        }
        private void AttachButtons(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                if (ctrl is Button btn) btn.Click += Button_Click;
                if (ctrl.HasChildren) AttachButtons(ctrl);
            }
        }

        #endregion

        #region ===== INPUT HANDLING =====

        private void Button_Click(object sender, EventArgs e)
        {
            if (sender is not Button btn) return;

            switch (btn.Name)
            {
                case "Btn_phimdaubang": Calculate(); break;
                case "Btn_phimdauxoaall": ClearAllFull(); break;
                case "Btn_phimdauxoa": DeleteLast(); break;
                default: ProcessInput(btn.Text.Trim()); break;
            }
        }

        private void Form_KeyPress(object sender, KeyPressEventArgs e)
        {
            char dec = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];

            if (char.IsDigit(e.KeyChar) || "+-*/()%^√".Contains(e.KeyChar))
            {
                ProcessInput(e.KeyChar.ToString());
                e.Handled = true;
            }
            else if (e.KeyChar == '.' || e.KeyChar == ',')
            {
                ProcessInput(dec.ToString());
                e.Handled = true;
            }
        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Calculate();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Back)
            {
                DeleteLast();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Delete)
            {
                ClearAll();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                CopyResult();
                e.SuppressKeyPress = true;
            }
        }

        private void ProcessInput(string value)
        {
            // [FIX 5]: Chống spam & Giới hạn độ dài
            if ((DateTime.Now - _lastInputTime).TotalMilliseconds < INPUT_DELAY) return;
            _lastInputTime = DateTime.Now;

            if (_expression.Length >= MAX_LENGTH) return;
            if (string.IsNullOrWhiteSpace(value)) return;

            value = value.Replace("×", "*").Replace("x", "*").Replace("X", "*").Replace("÷", "/");
            char dec = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];

            // [PRO 1]: Auto thêm ngoặc cho căn bậc 2
            if (value == "√")
            {
                if (_justCalculated) { _expression.Clear(); _justCalculated = false; }
                _expression.Append("√(");
                RefreshDisplay();
                return;
            }

            if (_justCalculated && char.IsDigit(value[0]))
            {
                _expression.Clear();
                _justCalculated = false;
            }

            if (value.Length == 1 && _operators.ContainsKey(value[0]))
            {
                _justCalculated = false; // Reset cờ để không bị lặp phép tính nhầm
                if (_expression.Length == 0)
                {
                    if (value[0] == '-') { _expression.Append(value); RefreshDisplay(); }
                    return;
                }

                if (_operators.ContainsKey(_expression[^1]) && _expression[^1] != '√')
                {
                    _expression[^1] = value[0];
                    RefreshDisplay();
                    return;
                }
            }

            if (value.Length == 1 && value[0] == dec && GetCurrentNumber().Contains(dec))
                return;

            _expression.Append(value);
            RefreshDisplay();
        }

        #endregion

        #region ===== CALCULATE =====

        private void Calculate()
        {
            if (_expression.Length == 0) return;

            try
            {
                // [PRO 2]: Lặp phép tính nếu user vừa tính xong và ấn '=' tiếp
                if (_justCalculated && _lastOp != '\0')
                {
                    _expression.Append(_lastOp).Append(_lastRightOperand.ToString(CultureInfo.InvariantCulture));
                }

                string rawExpression = _expression.ToString();
                double result = Evaluate(rawExpression);

                // Add expression
                ListBox1.Items.Add(rawExpression + " =");
                // Add result
                ListBox1.Items.Add(result.ToString("G15", CultureInfo.CurrentCulture));

                // [PRO 3]: Highlight kết quả và cuộn xuống
                ListBox1.SelectedIndex = ListBox1.Items.Count - 1;

                _expression.Clear();
                _expression.Append(result.ToString(CultureInfo.CurrentCulture));
                _justCalculated = true;
            }
            // [FIX 2]: Phân biệt Exception cực chuẩn
            catch (DivideByZeroException) { ShowError("Lỗi: Không thể chia cho 0"); }
            catch (ArithmeticException ex) { ShowError(ex.Message); }
            catch (Exception) { ShowError("Lỗi: Biểu thức không hợp lệ"); }
        }

        private double Evaluate(string expression)
        {
            var tokens = Tokenize(expression);
            var postfix = ToPostfix(tokens);
            return EvaluatePostfix(postfix);
        }

        #endregion

        #region ===== TOKENIZE =====


        private List<string> Tokenize(string expression)
        {
            var tokens = new List<string>();
            var number = new StringBuilder();
            char dec = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];

            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];

                if (char.IsDigit(c) || c == dec)
                {
                    number.Append(c);
                    continue;
                }

                if (c == '-' && (i == 0 || _operators.ContainsKey(expression[i - 1]) || expression[i - 1] == '('))
                {
                    number.Append(c);
                    continue;
                }

                if (number.Length > 0)
                {
                    tokens.Add(number.ToString());
                    number.Clear();
                }

                tokens.Add(c.ToString());
            }
            if (number.Length > 0) tokens.Add(number.ToString());
            return tokens;
        }
        #endregion

        #region ===== INFIX → POSTFIX =====

        private List<string> ToPostfix(List<string> tokens)
        {
            var output = new List<string>();
            var stack = new Stack<string>();

            foreach (var token in tokens)
            {
                if (double.TryParse(token, NumberStyles.Float, CultureInfo.CurrentCulture, out _))
                {
                    output.Add(token);
                }
                else if (token.Length == 1 && _operators.ContainsKey(token[0]))
                {
                    // [FIX 3]: Xử lý Associativity chuẩn (Right-associative cho ^ và √)
                    while (stack.Count > 0 &&
                           _operators.ContainsKey(stack.Peek()[0]) &&
                           (
                               (_operators[stack.Peek()[0]] > _operators[token[0]]) ||
                               (_operators[stack.Peek()[0]] == _operators[token[0]] && token[0] != '^' && token[0] != '√')
                           ))
                    {
                        output.Add(stack.Pop());
                    }
                    stack.Push(token);
                }
                else if (token == "(")
                {
                    stack.Push(token);
                }
                else if (token == ")")
                {
                    while (stack.Count > 0 && stack.Peek() != "(")
                        output.Add(stack.Pop());

                    if (stack.Count == 0) throw new Exception("Ngoặc không cân");
                    stack.Pop();
                }
            }

            while (stack.Count > 0)
            {
                if (stack.Peek() == "(") throw new Exception("Ngoặc không cân");
                output.Add(stack.Pop());
            }

            return output;
        }

        #endregion
        #region ===== POSTFIX EVALUATE =====

        private double EvaluatePostfix(List<string> postfix)
        {
            var values = new Stack<double>();

            foreach (var token in postfix)
            {
                if (double.TryParse(token, NumberStyles.Float, CultureInfo.CurrentCulture, out double num))
                {
                    values.Push(num);
                }
                else if (token.Length == 1 && _operators.ContainsKey(token[0]))
                {
                    if (token[0] == '√')
                    {
                        if (values.Count < 1) throw new Exception("Thiếu toán hạng");
                        double val = values.Pop();
                        // [FIX 1]: Chặn Sqrt số âm bẻ gãy UX
                        if (val < 0) throw new ArithmeticException("Lỗi: Không thể căn số âm");
                        values.Push(Math.Sqrt(val));
                        continue;
                    }

                    if (values.Count < 2) throw new Exception("Thiếu toán hạng");

                    double b = values.Pop();
                    double a = values.Pop();

                    // [PRO 2]: Lưu vết toán tử cuối cùng để lặp phép tính
                    _lastOp = token[0];
                    _lastRightOperand = b;

                    values.Push(token[0] switch
                    {
                        '+' => a + b,
                        '-' => a - b,
                        '*' => a * b,
                        '/' => b == 0 ? throw new DivideByZeroException() : a / b,
                        '%' => a * b / 100, // [FIX 2]: Giống máy tính Windows (Percentage)
                        '^' => Math.Pow(a, b),
                        _ => throw new InvalidOperationException()
                    });
                }
            }

            if (values.Count != 1) throw new Exception("Sai biểu thức");
            return values.Pop();
        }

        #endregion
        #region ===== UTIL =====

        private string GetCurrentNumber()
        {
            if (_expression.Length == 0) return string.Empty;

            var sb = new StringBuilder();
            for (int i = _expression.Length - 1; i >= 0; i--)
            {
                char c = _expression[i];
                if (_operators.ContainsKey(c) || c == '(' || c == ')') break;
                sb.Insert(0, c);
            }
            return sb.ToString();
        }
        private void DeleteLast()
        {
            if (_expression.Length == 0) return;
            _expression.Remove(_expression.Length - 1, 1);
            RefreshDisplay();
        }

        private void ClearAll()
        {
            _expression.Clear();
            _justCalculated = false;
            RefreshDisplay();
        }

        private void ClearAllFull()
        {
            _expression.Clear();
            _justCalculated = false;
            _lastOp = '\0';
            ListBox1.Items.Clear();
        }

        // [FIX 4]: RefreshDisplay cực chuẩn
        private void RefreshDisplay()
        {
            string text = _expression.ToString();

            if (ListBox1.Items.Count == 0)
            {
                ListBox1.Items.Add(text);
            }
            else if (_justCalculated)
            {
                ListBox1.Items.Add(text);
                _justCalculated = false;
            }
            else
            {
                ListBox1.Items[ListBox1.Items.Count - 1] = text;
            }

            ListBox1.TopIndex = ListBox1.Items.Count - 1;
        }

        private void ShowError(string message)
        {
            ListBox1.Items.Add(_expression.ToString());
            ListBox1.Items.Add(message);
            ListBox1.SelectedIndex = ListBox1.Items.Count - 1;

            _expression.Clear();
            _justCalculated = false;
            _lastOp = '\0'; // Xóa cache lặp phép tính
        }

        private void CopyResult()
        {
            if (ListBox1.Items.Count > 0)
            {
                string lastItem = ListBox1.Items[^1].ToString();
                if (!string.IsNullOrWhiteSpace(lastItem) && !lastItem.Contains("Lỗi"))
                {
                    Clipboard.SetText(lastItem);
                }
            }
        }

        #endregion
    }
}