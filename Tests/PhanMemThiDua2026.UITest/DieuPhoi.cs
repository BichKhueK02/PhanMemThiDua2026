using System.Diagnostics;
using System.Text;
namespace PhanMemThiDua2026.UITest
{
    /// <summary>
    /// Tiến trình điều phối: hỏi số lần chạy (1-400), chạy lần lượt từng lần trong tiến trình con
    /// (PhanMemThiDua2026.UITest.exe --lan k n đợt), hiện tiến độ, cho phép dừng, cuối cùng ghi + mở báo cáo tổng hợp.
    /// Tiến trình này KHÔNG nạp phần mềm (không gọi Program.Main) nên được tự do hiện cửa sổ.
    /// </summary>
    internal static class DieuPhoi
    {
        public const int SoLanToiDa = 400;
        public static int Chay()
        {
            ApplicationConfiguration.Initialize();
            var ch = CauHinh.Doc(out string loi);
            if (ch == null)
            {
                MessageBox.Show(loi, "Kiểm thử giao diện", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 1;
            }
            int? soLan = HoiSoLan();
            if (soLan == null) return 0;
            if (Process.GetProcessesByName("PhanMemThiDua2026").Length > 0)
            {
                var tl = MessageBox.Show("Phần mềm Thi đua đang mở.\nHãy tắt nó trước (phần mềm chỉ cho chạy một phiên).\n\nBấm OK sau khi đã tắt, hoặc Hủy để thôi.",
                    "Kiểm thử giao diện", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (tl != DialogResult.OK) return 0;
            }
            Application.Run(new FormTienDo(soLan.Value, ch));
            return 0;
        }
        // =============================================================== HỎI SỐ LẦN
        private static int? HoiSoLan()
        {
            using var f = new Form
            {
                Text = "Kiểm thử giao diện tự động",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false, MinimizeBox = false,
                AutoScaleMode = AutoScaleMode.Dpi,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Font = new Font("Segoe UI", 10F),
                Padding = new Padding(16),
            };
            var bang = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Dock = DockStyle.Fill };
            var nhan = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(420, 0),
                Text = $"Chạy bấm thử toàn bộ giao diện bao nhiêu lần? (1 - {SoLanToiDa})\n\n" +
                       "Mỗi lần: mở phần mềm, đăng nhập, bấm hết các nút, rồi tắt.\n" +
                       "Lưu ý: chạy trên dữ liệu THẬT của bản Release.",
                Margin = new Padding(0, 0, 0, 12),
            };
            bang.Controls.Add(nhan, 0, 0);
            bang.SetColumnSpan(nhan, 2);
            bang.Controls.Add(new Label { Text = "Số lần:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 8, 0) }, 0, 1);
            var so = new NumericUpDown { Minimum = 1, Maximum = SoLanToiDa, Value = 1, Width = 100, Anchor = AnchorStyles.Left };
            bang.Controls.Add(so, 1, 1);
            var nut = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 16, 0, 0) };
            var huy = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(90, 32) };
            var ok = new Button { Text = "Bắt đầu", DialogResult = DialogResult.OK, AutoSize = true, MinimumSize = new Size(90, 32) };
            nut.Controls.Add(huy);
            nut.Controls.Add(ok);
            bang.Controls.Add(nut, 0, 2);
            bang.SetColumnSpan(nut, 2);
            f.Controls.Add(bang);
            f.AcceptButton = ok;
            f.CancelButton = huy;
            f.Shown += (_, _) => { so.Select(0, so.Text.Length); so.Focus(); };
            if (f.ShowDialog() != DialogResult.OK) return null;
            return (int)Math.Clamp(so.Value, 1, SoLanToiDa);
        }
        // =============================================================== CỬA SỔ TIẾN ĐỘ + VÒNG CHẠY
        private sealed class FormTienDo : Form
        {
            private readonly int _tong;
            private readonly CauHinh _ch;
            private readonly string _dot = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            private readonly Label _trangThai = new() { AutoSize = true, MaximumSize = new Size(520, 0), Font = new Font("Segoe UI Semibold", 11F) };
            private readonly Label _ketQua = new() { AutoSize = true, MaximumSize = new Size(520, 0), Margin = new Padding(0, 8, 0, 0) };
            private readonly ProgressBar _thanh = new() { Width = 520, Height = 18, Margin = new Padding(0, 10, 0, 0) };
            private readonly Button _dungSau = new() { Text = "Dừng sau lần này", AutoSize = true, MinimumSize = new Size(140, 32) };
            private readonly Button _dungNgay = new() { Text = "Dừng ngay", AutoSize = true, MinimumSize = new Size(110, 32) };
            private volatile bool _yeuCauDung;
            private Process? _con;
            private readonly List<string> _tomTat = new();
            public FormTienDo(int tong, CauHinh ch)
            {
                _tong = tong;
                _ch = ch;
                Text = "Kiểm thử giao diện tự động";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.Manual;
                MaximizeBox = false;
                TopMost = true;
                ShowInTaskbar = true;
                AutoScaleMode = AutoScaleMode.Dpi;
                AutoSize = true;
                AutoSizeMode = AutoSizeMode.GrowAndShrink;
                Font = new Font("Segoe UI", 10F);
                Padding = new Padding(16);
                _thanh.Maximum = tong;
                var dong = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Dock = DockStyle.Fill };
                var nut = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
                nut.Controls.Add(_dungSau);
                nut.Controls.Add(_dungNgay);
                dong.Controls.AddRange(new Control[] { _trangThai, _thanh, _ketQua, nut });
                Controls.Add(dong);
                _dungSau.Click += (_, _) => { _yeuCauDung = true; _dungSau.Enabled = false; _dungSau.Text = "Sẽ dừng sau lần này"; };
                _dungNgay.Click += (_, _) =>
                {
                    _yeuCauDung = true;
                    _dungSau.Enabled = _dungNgay.Enabled = false;
                    try { if (_con is { HasExited: false }) _con.Kill(true); } catch { }
                };
                FormClosing += (_, e) =>
                {
                    if (_con is { HasExited: false })
                    {
                        if (MessageBox.Show(this, "Đang chạy kiểm thử. Dừng ngay và đóng?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                        { e.Cancel = true; return; }
                        _yeuCauDung = true;
                        try { _con.Kill(true); } catch { }
                    }
                };
                // góc dưới phải, không che giữa màn hình (phần mềm đang được bấm ở giữa)
                Load += (_, _) =>
                {
                    var vung = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
                    Location = new Point(vung.Right - Width - 12, vung.Bottom - Height - 12);
                };
                Shown += async (_, _) => await ChayAsync();
            }
            private async Task ChayAsync()
            {
                string exe = Environment.ProcessPath ?? Application.ExecutablePath;
                var batDau = DateTime.Now;
                int daChay = 0;
                for (int lan = 1; lan <= _tong && !_yeuCauDung; lan++)
                {
                    _trangThai.Text = $"Đang chạy lần {lan}/{_tong}...";
                    _thanh.Value = lan - 1;
                    await ChoDichVuVaPhienCuAsync();
                    var swLan = Stopwatch.StartNew();
                    string ghiChu = "";
                    try
                    {
                        _con = Process.Start(new ProcessStartInfo(exe, $"--lan {lan} {_tong} {_dot}")
                        {
                            UseShellExecute = false,
                            WorkingDirectory = AppContext.BaseDirectory,
                        });
                        if (_con == null) throw new InvalidOperationException("Không mở được tiến trình con");
                        var han = TimeSpan.FromMinutes(Math.Max(5, _ch.ThoiGianToiDaMoiLanPhut));
                        while (!_con.HasExited)
                        {
                            await Task.Delay(1000);
                            if (swLan.Elapsed > han)
                            {
                                ghiChu = $" — QUÁ {han.TotalMinutes:0} PHÚT, đã buộc dừng";
                                try { _con.Kill(true); } catch { }
                                break;
                            }
                            _trangThai.Text = $"Đang chạy lần {lan}/{_tong}... ({swLan.Elapsed:hh\\:mm\\:ss})";
                        }
                        try { _con.WaitForExit(10000); } catch { }
                    }
                    catch (Exception ex) { ghiChu = " — lỗi mở tiến trình: " + ex.Message; }
                    daChay = lan;
                    string tomTat = DocTomTat(BaoCao.TepLan(_dot, lan));
                    _tomTat.Add($"Lần {lan:000} ({swLan.Elapsed:hh\\:mm\\:ss}){ghiChu}: {tomTat}");
                    _ketQua.Text = $"Lần {lan}: {tomTat}{ghiChu}";
                    _thanh.Value = lan;
                    _con = null;
                }
                _trangThai.Text = _yeuCauDung && daChay < _tong ? $"Đã dừng sau {daChay}/{_tong} lần." : $"Xong {daChay}/{_tong} lần.";
                _dungSau.Enabled = _dungNgay.Enabled = false;
                string tepTongHop = GhiTongHop(daChay, DateTime.Now - batDau);
                DonTienTrinhSot();
                ChuongTrinh.MoTep(_tong == 1 && daChay == 1 ? BaoCao.TepLan(_dot, 1) : tepTongHop);
                Environment.Exit(0);   // xong hết => tắt luôn cửa sổ tiến độ (báo cáo đã mở bằng Notepad)
            }
            /// <summary>Còn sót phần mềm đang chạy dưới công cụ (lần bị buộc dừng...) => diệt để máy sạch sau khi kiểm thử.</summary>
            private static void DonTienTrinhSot()
            {
                int toi = Environment.ProcessId;
                foreach (var p in Process.GetProcessesByName("PhanMemThiDua2026.UITest"))
                {
                    try { if (p.Id != toi && !p.HasExited) p.Kill(true); } catch { }
                    finally { p.Dispose(); }
                }
            }
            /// <summary>Lần trước còn sót tiến trình / dịch vụ sao lưu-khôi phục đang chạy => chờ (tối đa 10 phút).</summary>
            private static async Task ChoDichVuVaPhienCuAsync()
            {
                var han = DateTime.Now.AddMinutes(10);
                while (DateTime.Now < han)
                {
                    bool con = Process.GetProcessesByName("ServiceBackup").Length > 0
                               || Process.GetProcessesByName("ServiceRestore").Length > 0
                               || Process.GetProcessesByName("PhanMemThiDua2026").Length > 0
                               || Process.GetProcessesByName("PhanMemThiDua2026.UITest").Length > 1;
                    if (!con) break;
                    await Task.Delay(2000);
                }
                await Task.Delay(2000);   // để Windows nhả hẳn khóa tệp CSDL / mutex của lần trước
            }
            /// <summary>Đọc dòng tổng kết từ báo cáo của một lần chạy.</summary>
            private static string DocTomTat(string tep)
            {
                try
                {
                    if (!File.Exists(tep)) return "KHÔNG CÓ BÁO CÁO";
                    var dong = File.ReadAllLines(tep, Encoding.UTF8);
                    int i = Array.FindLastIndex(dong, d => d.StartsWith("===== TỔNG KẾT"));
                    if (i < 0) return "KHÔNG CÓ TỔNG KẾT (tiến trình bị dừng giữa chừng)";
                    var dem = dong.Skip(i + 1).TakeWhile(d => d.Length > 0)
                                  .Where(d => d.StartsWith("  ")).Select(d => System.Text.RegularExpressions.Regex.Replace(d.Trim(), @"\s+", " "));
                    return string.Join(" · ", dem);
                }
                catch (Exception ex) { return "không đọc được báo cáo: " + ex.Message; }
            }
            private string GhiTongHop(int daChay, TimeSpan thoiGian)
            {
                string tep = Path.Combine(BaoCao.ThuMucNhatKy, $"uitest-{_dot}-TONGHOP.log");
                var sb = new StringBuilder();
                sb.AppendLine($"===== TỔNG HỢP KIỂM THỬ GIAO DIỆN - {DateTime.Now:dd/MM/yyyy HH:mm:ss} =====");
                sb.AppendLine($"Số lần yêu cầu: {_tong} · Đã chạy: {daChay} · Tổng thời gian: {thoiGian:hh\\:mm\\:ss}");
                sb.AppendLine();
                foreach (var t in _tomTat) sb.AppendLine(t);
                sb.AppendLine();
                // Gom danh sách lỗi/treo của từng lần (đếm số lần lặp lại cùng một lỗi)
                var demLoi = new Dictionary<string, List<int>>();
                for (int lan = 1; lan <= daChay; lan++)
                {
                    try
                    {
                        string f = BaoCao.TepLan(_dot, lan);
                        if (!File.Exists(f)) continue;
                        var dong = File.ReadAllLines(f, Encoding.UTF8);
                        int i = Array.FindLastIndex(dong, d => d.StartsWith("DANH SÁCH LỖI / TREO"));
                        if (i < 0) continue;
                        foreach (var d in dong.Skip(i + 1).TakeWhile(d => d.Length > 0))
                        {
                            string t = d.Trim();
                            if (!t.StartsWith("[")) continue;   // chỉ lấy dòng tiêu đề lỗi
                            t = System.Text.RegularExpressions.Regex.Replace(t, @" \(\d+ ms\)", "");
                            if (!demLoi.TryGetValue(t, out var ds)) demLoi[t] = ds = new List<int>();
                            ds.Add(lan);
                        }
                    }
                    catch { }
                }
                if (demLoi.Count == 0) sb.AppendLine("Không phát hiện lỗi / treo trong các lần chạy.");
                else
                {
                    sb.AppendLine($"CÁC LỖI / TREO GẶP PHẢI ({demLoi.Count} loại):");
                    foreach (var kv in demLoi.OrderByDescending(k => k.Value.Count))
                        sb.AppendLine($"  [{kv.Value.Count}/{daChay} lần] {kv.Key}   (lần: {string.Join(", ", kv.Value.Take(20))}{(kv.Value.Count > 20 ? ", ..." : "")})");
                }
                sb.AppendLine();
                sb.AppendLine("Báo cáo chi tiết từng lần:");
                for (int lan = 1; lan <= daChay; lan++) sb.AppendLine("  " + BaoCao.TepLan(_dot, lan));
                try { File.WriteAllText(tep, sb.ToString(), new UTF8Encoding(true)); } catch { }
                return tep;
            }
        }
    }
}
