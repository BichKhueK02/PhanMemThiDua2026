///TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
using Microsoft.Data.Sqlite;
namespace PhanMemThiDua2026
{
    //TrungKien- Yeu Meo Cam - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026 - https://github.com/BichKhueK02/PhanMemThiDua2026
    public partial class Form17_XuatDataSangTK : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;
        private static string TABLE_CBCS => Module_CheDoDoiTuong.ThiDuaThang(CheDoDoiTuong.CBCS);
        // Thông báo kết quả hiện bao lâu rồi tự ẩn (ms) - sửa ở đây nếu muốn hiện lâu hơn
        private const int THOI_GIAN_AN_THONG_BAO_MS = 400;
        private int _thongBaoCounter = 0;
        // Đọc chế độ phần mềm MỘT lần khi mở form (trước đây gọi lại nhiều lần ở mỗi thao tác)
        private readonly bool _laTanBinh;
        // true khi đang ghi CSDL ở luồng nền => chặn đóng form giữa chừng
        private volatile bool _dangXuat;
        // Mapping hiển thị -> tên cột DB (dùng chung cho mọi lần mở form, không tạo lại mỗi lần)
        private static readonly Dictionary<string, string> mapCBCS = new() {
            {"Tháng 12 (Năm cũ)","Thang_12_Nam_Cu"},
            {"Tháng 1","Thang_1"},
            {"Tháng 2","Thang_2"},
            {"Tháng 3","Thang_3"},
            {"Tháng 4","Thang_4"},
            {"Tháng 5","Thang_5"},
            {"6 Tháng đầu năm","Sau_Thang_Dau_Nam"},
            {"Tháng 6","Thang_6"},
            {"Tháng 7","Thang_7"},
            {"Tháng 8","Thang_8"},
            {"Tháng 9","Thang_9"},
            {"Tháng 10","Thang_10"},
            {"Tháng 11","Thang_11"},
            {"Tổng kết năm","TongKet_Nam"}
        };
        // Tân binh / Học viên: mốc chọn theo lịch xét (Module_LichXet) - Học viên tùy tháng đã cài
        private static Dictionary<string, string> mapTanBinh => Module_LichXet.DanhSachChon(Module_CheDoDoiTuong.Hien);
        public Form17_XuatDataSangTK()
        {
            InitializeComponent();
            Module_BieuTuongNut.ApDung(this);   // biểu tượng nút: Hiện đại = tự vẽ, Cổ điển = ảnh gốc nhúng trong exe
            Module_GiaoDienForm17Web.ApDung(this); // giao diện kiểu web (Module_GiaoDienForm17Web.cs) - chỉ trình bày
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            _laTanBinh = LaPhienBanTanBinh();
            LoadComboBoxThang();
            InitToolTips();
        }
        private void Form17_Load(object sender, EventArgs e)
        {
            // Ẩn nhãn thông báo khi form khởi động
            if (toolStripStatusLabel1_ThongBaoThanhCong != null)
            {
                toolStripStatusLabel1_ThongBaoThanhCong.Visible = false;
                toolStripStatusLabel1_ThongBaoThanhCong.Text = "";
            }
            // Hiển thị phiên bản và chế độ
            if (toolStripStatusLabel1_PhienBanPhanMem != null)
            {
                string phienBan = Module_PhienBan.GetFullVersion();
                string cheDo = _laTanBinh ? $"Chế độ {Module_TaiKhoan.TenTB_Hoa}" : "Chế độ CBCS";
                toolStripStatusLabel1_PhienBanPhanMem.Text = $"Phiên bản {phienBan} - {cheDo}";
            }
        }
        // Đang ghi dữ liệu thì không cho đóng form (tránh đóng giữa chừng khi luồng nền chưa xong)
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (_dangXuat && e.CloseReason == CloseReason.UserClosing)
                e.Cancel = true;
        }
        private static string TryDec(string? val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            try { return Module_BaoMatAES.GiaiMa(val).Trim(); }
            catch { return val; } // Nếu không mã hóa thì trả về gốc
        }
        private void LoadComboBoxThang()
        {
            var dict = _laTanBinh ? mapTanBinh : mapCBCS;
            if (dict.Count <= 0) return;
            var cb = comboBox1_ChonThangCanXuat;
            // Nạp một lần bằng AddRange giữa BeginUpdate/EndUpdate (không vẽ lại từng mục)
            cb.BeginUpdate();
            try
            {
                cb.Items.Clear();
                cb.Items.AddRange(dict.Keys.Cast<object>().ToArray());
                int indexMacDinh = 0;
                // Chỉ CBCS mới gợi ý tháng hiện tại
                if (!_laTanBinh)
                {
                    int thangHienTai = DateTime.Now.Month;
                    string chuoiCanTim = thangHienTai == 12 ? "Tháng 12 (Năm cũ)" : $"Tháng {thangHienTai}";
                    int viTri = cb.Items.IndexOf(chuoiCanTim);
                    if (viTri >= 0) indexMacDinh = viTri;
                }
                cb.SelectedIndex = indexMacDinh;
            }
            finally
            {
                cb.EndUpdate();
            }
        }
        // Hiện thông báo trên thanh trạng thái
        private void HienThongBao(string noiDung, Color mau)
        {
            var nhan = toolStripStatusLabel1_ThongBaoThanhCong;
            if (nhan == null || IsDisposed) return;
            nhan.ForeColor = mau;
            nhan.Text = noiDung;
            nhan.Visible = true;
        }
        // Hàm đếm ngược thời gian độc lập không làm đơ Form
        private async void AnThongBaoSauDelay(int delayMs)
        {
            int currentCounter = Interlocked.Increment(ref _thongBaoCounter);
            try { await Task.Delay(delayMs); }
            catch { return; }
            // Form đã đóng hoặc đã có thông báo mới hơn => không đụng vào nhãn
            if (IsDisposed || currentCounter != Volatile.Read(ref _thongBaoCounter)) return;
            var nhan = toolStripStatusLabel1_ThongBaoThanhCong;
            if (nhan != null)
            {
                // Ẩn và xóa text sau khi hết thời gian chờ
                nhan.Visible = false;
                nhan.Text = "";
            }
        }
        private async void kryptonButton_XuatDuLieuSangThongKe_Click(object sender, EventArgs e)
        {
            // 1. LƯU TRẠNG THÁI GỐC CỦA NÚT
            string textBanDau = kryptonButton_XuatDuLieuSangThongKe.Values.Text;
            Image? anhBanDau = kryptonButton_XuatDuLieuSangThongKe.Values.Image;
            try
            {
                // 2. KIỂM TRA ĐIỀU KIỆN TRƯỚC TIÊN (Trên UI thread)
                if (comboBox1_ChonThangCanXuat.SelectedIndex < 0)
                {
                    MessageBox.Show("Vui lòng chọn tháng cần xuất.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!File.Exists(_csdl2Path) || !File.Exists(_csdl4Path))
                {
                    MessageBox.Show("Không tìm thấy CSDL.", Module_HeThong.TieuDe_Loi,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // Đọc các giá trị phụ thuộc giao diện TRƯỚC khi chuyển trạng thái (nếu lỗi thì chưa khóa nút)
                string tenBang = _laTanBinh ? Module_CheDoDoiTuong.BangThiDuaThang : TABLE_CBCS;   // ThiDuaThang_TanBinh / _HocVien
                string tenCot = GetTenCotThang(_laTanBinh);
                string thangHienThi = GetTenThangHienThi();
                // 3. THIẾT LẬP TRẠNG THÁI "ĐANG XỬ LÝ"
                _dangXuat = true;
                kryptonButton_XuatDuLieuSangThongKe.Enabled = false;
                kryptonButton_XuatDuLieuSangThongKe.Values.Text = "Đang xuất...";
                kryptonButton_XuatDuLieuSangThongKe.Values.Image = null;
                HienThongBao("Đang đồng bộ dữ liệu...", Color.Black);
                // 4. ĐẨY VIỆC NẶNG XUỐNG LUỒNG NGẦM (Tránh đơ Form). await trả quyền cho UI nên trạng thái ở trên được vẽ ngay.
                string thongBao = await Task.Run(() => XuatDuLieuNen(tenBang, tenCot, thangHienThi, _laTanBinh));
                // 5. XỬ LÝ KẾT QUẢ KHI TASK HOÀN TẤT
                if (thongBao.Contains("thành công", StringComparison.OrdinalIgnoreCase))
                {
                    HienThongBao($"✔ {thongBao} lúc {DateTime.Now:HH:mm:ss}", Color.DarkGreen);
                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Xuất dữ liệu phân loại sang Thống kê",
                        $"Bảng: {tenBang} | Cột: {tenCot} ({thangHienThi})");
                }
                else
                {
                    HienThongBao("✘ Xuất dữ liệu thất bại!", Color.Red);
                    Module_NhatKy.GhiNhatKy(Module_TaiKhoan.TenTaiKhoan_RAM, "Lỗi xuất dữ liệu phân loại sang Thống kê", thongBao);
                    MessageBox.Show(thongBao, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                HienThongBao("✘ Phát sinh lỗi!", Color.Red);
                MessageBox.Show("Lỗi chương trình: " + ex.Message, Module_HeThong.TieuDe_Loi, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _dangXuat = false;
                // 6. KHÔI PHỤC NÚT (Luôn chạy dù lỗi hay không)
                if (!IsDisposed)
                {
                    kryptonButton_XuatDuLieuSangThongKe.Values.Text = textBanDau;
                    kryptonButton_XuatDuLieuSangThongKe.Values.Image = anhBanDau;
                    kryptonButton_XuatDuLieuSangThongKe.Enabled = true;
                    AnThongBaoSauDelay(THOI_GIAN_AN_THONG_BAO_MS);
                }
            }
        }
        // Toàn bộ việc đọc/ghi CSDL chạy ở luồng nền. Không đụng tới control nào.
        private string XuatDuLieuNen(string tenBang, string tenCot, string thangHienThi, bool laTanBinh)
        {
            try
            {
                // MoKetNoiDoc: Pooling=false => đóng là nhả hẳn tệp .db (không kẹt sao lưu/khôi phục)
                using var cn2 = Module_DanduongGPS.MoKetNoiDoc(_csdl2Path);
                using var cn4 = Module_DanduongGPS.MoKetNoiDoc(_csdl4Path);
                GhiThiDuaThang(cn2, cn4, tenBang, tenCot, laTanBinh);
                // Cập nhật thống kê tập thể bằng chính 2 kết nối đang mở (trước đây mở thêm 2 kết nối pooling ở luồng UI)
                // CHỈ CBCS: ThongKe_PhanLoaiTapThe_CBCS dùng cột tháng của CBCS; Tân binh có cột trùng tên (Thang_3..Thang_6)
                // nên nếu ghi sẽ đè mất kết quả tập thể của CBCS.
                if (!laTanBinh) NapThongKePhanLoaiTapThe(cn2, cn4, tenCot);
                // Trả về chuỗi chứa từ khóa "thành công" để UI nhận diện
                return $"Xuất dữ liệu sang {thangHienThi} thành công";
            }
            catch (Exception exDB)
            {
                return "Lỗi CSDL: " + exDB.Message;
            }
        }
        private void GhiThiDuaThang(SqliteConnection cn2, SqliteConnection cn4, string tenBang, string tenCot, bool laTanBinh)
        {
            // Lấy dữ liệu 2 bên (cùng theo thứ tự ID). KHÔNG gộp người trùng Số hiệu.
            var danhSach = LayDanhSachCBCS(cn2);
            var dich = LayDanhSachDich(cn4, tenBang);
            // Ghép MỘT-MỘT bằng đúng quy tắc của Form15 (Module_DongBoThiDua): có rồi thì cập nhật, chưa có mới thêm
            int[] ghep = Module_DongBoThiDua.GhepMotMot(
                danhSach.Select(x => new Module_DongBoThiDua.Nguoi(x.HoVaTen, x.SoHieu, x.DonVi)).ToList(),
                dich.Select(d => d.Nguoi).ToList(),
                out _);
            bool laTongKetNam = tenCot == "TongKet_Nam";
            using var tran = cn4.BeginTransaction();
            using var cmdUpd = new SqliteCommand($"UPDATE {tenBang} SET [{tenCot}]=@v WHERE ID=@id", cn4, tran);
            var pUv = cmdUpd.Parameters.Add("@v", SqliteType.Text);
            var pUid = cmdUpd.Parameters.Add("@id", SqliteType.Integer);
            // Dòng mới có đủ TinhTrang + TS_Loai giống Form15 => không phải chờ đồng bộ mới hiện đúng
            using var cmdIns = new SqliteCommand($@"INSERT INTO {tenBang}(HoVaTen,SoHieu,DonVi,TinhTrang,TS_Loai1,TS_Loai2,TS_Loai3,TS_Loai4,[{tenCot}])
VALUES(@hvt,@sh,@dv,@tt,'0','0','0','0',@gt)", cn4, tran);
            var pHvt = cmdIns.Parameters.Add("@hvt", SqliteType.Text);
            var pSh = cmdIns.Parameters.Add("@sh", SqliteType.Text);
            var pDv = cmdIns.Parameters.Add("@dv", SqliteType.Text);
            var pGt = cmdIns.Parameters.Add("@gt", SqliteType.Text);
            cmdIns.Parameters.Add("@tt", SqliteType.Text).Value = Module_HeThong.TT_DANG_CONG_TAC;
            for (int i = 0; i < danhSach.Count; i++)
            {
                var r = danhSach[i];
                string phanLoai = Module_BaoMatAES.GiaiMa(r.PhanLoaiMaHoa);
                string giaTri = laTongKetNam ? ChuyenTongKetNam(phanLoai) : ChuyenLoaiSangSo(phanLoai);
                if (ghep[i] >= 0)
                {
                    pUv.Value = giaTri;
                    pUid.Value = dich[ghep[i]].Id;
                    cmdUpd.ExecuteNonQuery();
                }
                else
                {
                    // Đẩy dữ liệu ĐÃ MÃ HÓA nguyên gốc vào để bảo mật; điểm số lưu Plain Text
                    pHvt.Value = r.HoVaTen_MaHoa;
                    pSh.Value = r.SoHieu_MaHoa;
                    pDv.Value = r.DonVi_MaHoa;
                    pGt.Value = giaTri;
                    cmdIns.ExecuteNonQuery();
                }
            }
            if (laTanBinh)
            {
                // CapNhatTongLoaiTanBinh(cn4, tran);
            }
            tran.Commit();
        }
        private static string DocChuoi(SqliteDataReader rd, int i)
            => rd.IsDBNull(i) ? "" : (rd.GetString(i) ?? "");
        private static List<RecordCBCS> LayDanhSachCBCS(SqliteConnection cn)
        {
            List<RecordCBCS> list = new();
            using var cmd = new SqliteCommand($"SELECT HoVaTen,SoHieu,DonVi,PhanLoai FROM {Module_TaiKhoan.BangDanhSach} ORDER BY ID", cn);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                // Đọc theo chỉ số cột (nhanh hơn tra tên cột ở mỗi dòng)
                string rawHoTen = DocChuoi(rd, 0).Trim();
                string rawSoHieu = DocChuoi(rd, 1).Trim();
                string rawDonVi = DocChuoi(rd, 2).Trim();
                // Giải mã an toàn: GiaiMa trả "" khi không giải được nên dùng lại chuỗi gốc (TryDec cũ không bao giờ vào nhánh catch)
                string hoTen = Module_DongBoThiDua.GiaiMaAnToan(rawHoTen);
                string soHieu = Module_DongBoThiDua.GiaiMaAnToan(rawSoHieu);
                if (Module_DongBoThiDua.BoQua(hoTen, soHieu)) continue; // cùng luật với Form15
                list.Add(new RecordCBCS
                {
                    // Lưu bản mã hóa gốc để dành cho Insert
                    HoVaTen_MaHoa = rawHoTen,
                    SoHieu_MaHoa = rawSoHieu,
                    DonVi_MaHoa = rawDonVi,
                    // Bản rõ dùng để ghép người
                    HoVaTen = hoTen,
                    SoHieu = soHieu,
                    DonVi = Module_DongBoThiDua.GiaiMaAnToan(rawDonVi),
                    PhanLoaiMaHoa = DocChuoi(rd, 3)
                });
            }
            return list;
        }
        // Dòng hiện có ở CSDL4 (bản rõ) theo thứ tự ID để ghép với danh sách gốc
        private static List<(int Id, Module_DongBoThiDua.Nguoi Nguoi)> LayDanhSachDich(SqliteConnection cn, string table)
        {
            var list = new List<(int, Module_DongBoThiDua.Nguoi)>();
            using var cmd = new SqliteCommand($"SELECT ID,HoVaTen,SoHieu,DonVi FROM {table} ORDER BY ID", cn);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add((rd.GetInt32(0), new Module_DongBoThiDua.Nguoi(
                    Module_DongBoThiDua.GiaiMaAnToan(DocChuoi(rd, 1)),
                    Module_DongBoThiDua.GiaiMaAnToan(DocChuoi(rd, 2)),
                    Module_DongBoThiDua.GiaiMaAnToan(DocChuoi(rd, 3)))));
            }
            return list;
        }
        // Chuyển loại
        private static string ChuyenLoaiSangSo(string v) => v switch
        {
            Module_HeThong.Loai_1 => "1",
            Module_HeThong.Loai_2 => "2",
            Module_HeThong.Loai_3 => "3",
            Module_HeThong.Loai_4 => "4",
            _ => ""
        };
        private static string ChuyenTongKetNam(string v) => v switch
        {
            Module_HeThong.Loai_1 => Module_HeThong.PL_CSTD,
            Module_HeThong.Loai_2 => Module_HeThong.PL_CSTT,
            Module_HeThong.Loai_3 => Module_HeThong.PL_HTNV,
            Module_HeThong.Loai_4 => Module_HeThong.PL_KHTNV,
            _ => ""
        };
        private string GetTenCotThang(bool laTanBinh)
        {
            var dict = laTanBinh ? mapTanBinh : mapCBCS;
            if (!dict.TryGetValue(comboBox1_ChonThangCanXuat.Text.Trim(), out string? value))
                throw new Exception("Không xác định được cột tháng cần xuất.");
            return value;
        }
        private string GetTenThangHienThi()
        {
            if (comboBox1_ChonThangCanXuat.SelectedItem == null)
                return "";
            return comboBox1_ChonThangCanXuat.SelectedItem.ToString()!.Trim();
        }
        private static bool LaPhienBanTanBinh()
        {
            string pb = Module_TaiKhoan.LayPhienBanPhanMem();
            return pb.LaCheDoTanBinh();
        }
        // Cấu hình ToolTip (Gợi ý giao diện) chuẩn UX
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = Module_HeThong.Goi_Y_Thao_Tac;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.AutoPopDelay = 3000;
            toolTip1.InitialDelay = 400;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;
            toolTip1.SetToolTip(comboBox1_ChonThangCanXuat, "Lựa chọn mốc thời gian (Tuần/Tháng/Năm) để đồng bộ kết quả thi đua");
        }
        // Thống kê tập thể: chạy ở luồng nền, dùng lại 2 kết nối đã mở (Pooling=false). Lỗi chỉ ghi Debug, không làm hỏng việc xuất chính.
        private static void NapThongKePhanLoaiTapThe(SqliteConnection cn2, SqliteConnection cn4, string tenCot)
        {
            if (string.IsNullOrWhiteSpace(tenCot))
                return;
            try
            {
                // 1. Đọc dữ liệu duy nhất tại ID = 1 từ bảng ThongTin (CSDL2)
                using var cmd2 = new SqliteCommand("SELECT LoaiDeNghi FROM ThongTin WHERE ID = 1 LIMIT 1", cn2);
                object? rawLoaiDeNghiObj = cmd2.ExecuteScalar();
                if (rawLoaiDeNghiObj == null || rawLoaiDeNghiObj == DBNull.Value)
                    return;
                string? rawLoaiDeNghi = rawLoaiDeNghiObj.ToString();
                // 2. Giải mã dữ liệu CSDL2
                string loaiGiaiMa = TryDec(rawLoaiDeNghi);
                // 3. Ánh xạ sang ký hiệu thi đua tập thể (truyền tenCot vào để xử lý riêng TongKet_Nam)
                string giatriAnhXa = AnhXaLoaiDeNghiTapThe(loaiGiaiMa, tenCot);
                // 4. Mã hóa lại giá trị đã xử lý để lưu vào CSDL4
                string giatriMaHoaDuaVaoCSDL = string.IsNullOrEmpty(giatriAnhXa)
                    ? ""
                    : Module_BaoMatAES.MaHoa(giatriAnhXa);
                using var tr4 = cn4.BeginTransaction();
                // 5. Kiểm tra sự tồn tại của ID = 1 trong bảng ThongKe_PhanLoaiTapThe_CBCS (CSDL4)
                using var cmdCheck = new SqliteCommand("SELECT COUNT(1) FROM ThongKe_PhanLoaiTapThe_CBCS WHERE ID = 1", cn4, tr4);
                bool isExisted = Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0;
                // 6. Cập nhật động theo cột (tenCot) được truyền từ ComboBox
                string sql = isExisted
                    ? $"UPDATE ThongKe_PhanLoaiTapThe_CBCS SET [{tenCot}] = @val WHERE ID = 1"
                    : $"INSERT INTO ThongKe_PhanLoaiTapThe_CBCS (ID, [{tenCot}]) VALUES (1, @val)";
                using var cmdGhi = new SqliteCommand(sql, cn4, tr4);
                cmdGhi.Parameters.AddWithValue("@val", giatriMaHoaDuaVaoCSDL);
                cmdGhi.ExecuteNonQuery();
                tr4.Commit();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi NapThongKePhanLoaiTapThe: " + ex.Message);
            }
        }
        // Hàm hỗ trợ ánh xạ chuỗi (chỉ ánh xạ khi là Tổng kết năm)
        private static string AnhXaLoaiDeNghiTapThe(string input, string tenCot)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            string strInput = input.Trim();
            // Chỉ thực hiện chuyển đổi ký hiệu khi chọn cột Tổng kết năm
            if (tenCot == "TongKet_Nam")
            {
                return strInput switch
                {
                    Module_HeThong.Loai_1 => Module_HeThong.XLDV_DVQT,
                    Module_HeThong.Loai_2 => Module_HeThong.XLDV_DVTT,
                    Module_HeThong.Loai_3 => Module_HeThong.XLDV_HTNV,
                    Module_HeThong.Loai_4 => Module_HeThong.XLDV_KHTNV,
                    Module_HeThong.XLDV_KHONG_XET => Module_HeThong.XLDV_KHONG_XET,
                    _ => strInput
                };
            }
            // Các tháng khác và 6 tháng đầu năm giữ nguyên giá trị gốc ("Loại 1", "Loại 2",...)
            return strInput;
        }
        // Model dữ liệu (chứa song song 2 bản Mật/Rõ)
        private sealed class RecordCBCS
        {
            public string HoVaTen { get; set; } = "";
            public string SoHieu { get; set; } = "";
            public string DonVi { get; set; } = "";
            public string HoVaTen_MaHoa { get; set; } = "";
            public string SoHieu_MaHoa { get; set; } = "";
            public string DonVi_MaHoa { get; set; } = "";
            public string PhanLoaiMaHoa { get; set; } = "";
        }
    }
}