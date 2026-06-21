using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace PhanMemThiDua2026
{
    public class Form_Loading : Form
    {
        private Label lblMessage;
        private System.Windows.Forms.Timer animationTimer;
        private int currentAngle = 0;
        private int _currentPercent = 0;

        private bool _autoSimulate = true;
        private float _virtualProgress = 0f;

        // Cấu hình màu sắc UI
        private readonly Color ThemeColor = Color.FromArgb(41, 128, 185); // Xanh dương
        private readonly Color TrackColor = Color.FromArgb(220, 220, 220); // Xám nhạt
        private readonly Color TextColor = Color.FromArgb(64, 64, 64);     // Xám đậm
        private readonly Font MessageFont = new Font("Segoe UI", 10F, FontStyle.Regular); // Font chữ thông báo

        // Khai báo hàm API Windows để bo góc Form
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse, // Độ rộng của góc bo (Đường kính)
            int nHeightEllipse // Chiều cao của góc bo (Đường kính)
        );

        public Form_Loading(string message = "Đang xử lý dữ liệu...")
        {
            // 1. Cấu hình Form
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.WhiteSmoke;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.DoubleBuffered = true;

            // 2. Khởi tạo Label
            lblMessage = new Label
            {
                Text = message,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = MessageFont,
                ForeColor = TextColor,
                BackColor = Color.Transparent
            };
            this.Controls.Add(lblMessage);

            // 3. Tính toán kích thước tự động (Auto-Size)
            CapNhatKichThuocForm(message);

            // 4. Khởi tạo Timer
            animationTimer = new System.Windows.Forms.Timer { Interval = 15 };
            animationTimer.Tick += (s, e) =>
            {
                currentAngle = (currentAngle + 8) % 360;

                if (_autoSimulate && _currentPercent < 99)
                {
                    _virtualProgress += (99f - _virtualProgress) * 0.015f;
                    _currentPercent = (int)_virtualProgress;
                }

                this.Invalidate();
            };

            this.Shown += (s, e) => animationTimer.Start();
            this.FormClosing += (s, e) => animationTimer.Stop();
        }

        // HÀM TỰ ĐỘNG ĐO VÀ ĐIỀU CHỈNH KÍCH THƯỚC FORM
        private void CapNhatKichThuocForm(string text)
        {
            using (Graphics g = this.CreateGraphics())
            {
                SizeF textSize = g.MeasureString(text, MessageFont);

                int newWidth = Math.Max(240, (int)textSize.Width + 80);

                this.Size = new Size(newWidth, 160);

                lblMessage.Size = new Size(this.Width - 20, 40);
                lblMessage.Location = new Point(10, 110);

                // ⭐ SỬA LỖI 2 VIỀN: Truyền tham số đường kính bo góc là 20
                this.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 20, 20));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Bật khử răng cưa

            // ⭐ SỬA LỖI 2 VIỀN: Truyền tham số Bán kính (Radius) là 10 (Đường kính = 20, khớp 100% với hàm Region ở trên)
            using (Pen borderPen = new Pen(Color.LightGray, 1))
            {
                g.DrawPath(borderPen, GetRoundRectPath(new Rectangle(0, 0, this.Width - 1, this.Height - 1), 10));
            }

            // --- VẼ VÒNG TRÒN ---
            int spinnerSize = 65;
            int penThickness = 6;
            Rectangle spinnerRect = new Rectangle((this.Width - spinnerSize) / 2, 25, spinnerSize, spinnerSize);

            // Đường ray (màu xám nhạt)
            using (Pen trackPen = new Pen(TrackColor, penThickness))
            {
                g.DrawEllipse(trackPen, spinnerRect);
            }

            // Spinner (vòng cung chạy)
            using (Pen spinnerPen = new Pen(ThemeColor, penThickness))
            {
                spinnerPen.StartCap = LineCap.Round;
                spinnerPen.EndCap = LineCap.Round;
                g.DrawArc(spinnerPen, spinnerRect, currentAngle, 100);
            }

            // --- VẼ PHẦN TRĂM ---
            string percentText = $"{_currentPercent}%";
            using (Font percentFont = new Font("Segoe UI", 11F, FontStyle.Bold))
            using (Brush percentBrush = new SolidBrush(ThemeColor))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(percentText, percentFont, percentBrush, spinnerRect, sf);
            }
        }

        private GraphicsPath GetRoundRectPath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        public void CapNhatThongBao(string txt)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    lblMessage.Text = txt;
                    CapNhatKichThuocForm(txt);
                }));
            }
            else
            {
                lblMessage.Text = txt;
                CapNhatKichThuocForm(txt);
            }
        }

        public void CapNhatPhanTram(int percent)
        {
            _autoSimulate = false;
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            if (this.InvokeRequired) this.Invoke(new Action(() => { _currentPercent = percent; }));
            else _currentPercent = percent;
        }
    }
}