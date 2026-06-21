using Microsoft.Data.Sqlite;

namespace PhanMemThiDua2026
{
    public partial class Form17_XuatDataSangTK : Form
    {
        private readonly string _csdl2Path = Module_DanduongGPS.DuongDanCSDL2;
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;
        private const string TABLE_CBCS = "ThiDuaThang";
        private const string TABLE_TANBINH = "ThiDuaThang_TanBinh";

        // Mapping hiển thị -> tên cột DB
        private readonly Dictionary<string, string> mapCBCS = new()
        {
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
        private readonly Dictionary<string, string> mapTanBinh = new()
        {
            // Cụm 1: Quá trình Tháng 2 -> Chốt kết quả Tháng 3
            {"Tuần 1 - Tháng 2", "Tuan_1_T2"},
            {"Tuần 2 - Tháng 2", "Tuan_2_T2"},
            {"Tuần 3 - Tháng 2", "Tuan_3_T2"},
            {"Tuần 4 - Tháng 2", "Tuan_4_T2"},
            {"Kết quả Tháng 3", "Thang_3"},

            // Cụm 2: Quá trình Tháng 3 -> Chốt kết quả Tháng 4
            {"Tuần 1 - Tháng 3", "Tuan_1_T3"},
            {"Tuần 2 - Tháng 3", "Tuan_2_T3"},
            {"Tuần 3 - Tháng 3", "Tuan_3_T3"},
            {"Tuần 4 - Tháng 3", "Tuan_4_T3"},
            {"Kết quả Tháng 4", "Thang_4"},

            // Cụm 3: Quá trình Tháng 4 -> Chốt kết quả Tháng 5
            {"Tuần 1 - Tháng 4", "Tuan_1_T4"},
            {"Tuần 2 - Tháng 4", "Tuan_2_T4"},
            {"Tuần 3 - Tháng 4", "Tuan_3_T4"},
            {"Tuần 4 - Tháng 4", "Tuan_4_T4"},
            {"Kết quả Tháng 5", "Thang_5"},

            // Cụm 4: Quá trình Tháng 5 -> Chốt kết quả Tháng 6
            {"Tuần 1 - Tháng 5", "Tuan_1_T5"},
            {"Tuần 2 - Tháng 5", "Tuan_2_T5"},
            {"Tuần 3 - Tháng 5", "Tuan_3_T5"},
            {"Tuần 4 - Tháng 5", "Tuan_4_T5"},
            {"Kết quả Tháng 6", "Thang_6"}
        };
        public Form17_XuatDataSangTK()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            LoadComboBoxThang();
            InitToolTips();
        }
        private void Form17_Load(object sender, EventArgs e)
        {

        }
        // =====================================================
        // Hàm hỗ trợ giải mã an toàn (tránh văng lỗi do Data rác)
        // =====================================================
        private string TryDec(string val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            try { return BaoMatAES.GiaiMa(val).Trim(); }
            catch { return val; } // Nếu không mã hóa thì trả về gốc
        }
        // =====================================================
        // Load combobox tháng
        // =====================================================
        // =====================================================
        // Load combobox tháng
        // =====================================================
        private void LoadComboBoxThang()
        {
            comboBox1_ChonThangCanXuat.Items.Clear();

            bool laTanBinh = LaPhienBanTanBinh();

            var dict = laTanBinh ? mapTanBinh : mapCBCS;

            foreach (var key in dict.Keys)
            {
                comboBox1_ChonThangCanXuat.Items.Add(key);
            }

            if (comboBox1_ChonThangCanXuat.Items.Count <= 0)
                return;

            int indexMacDinh = 0;

            // =====================================================
            // Chỉ CBCS mới gợi ý tháng hiện tại
            // =====================================================
            if (!laTanBinh)
            {
                int thangHienTai = DateTime.Now.Month;

                string chuoiCanTim =
                    thangHienTai == 12
                    ? "Tháng 12 (Năm cũ)"
                    : $"Tháng {thangHienTai}";

                for (int i = 0; i < comboBox1_ChonThangCanXuat.Items.Count; i++)
                {
                    string item =
                        comboBox1_ChonThangCanXuat.Items[i]?.ToString() ?? "";

                    if (item == chuoiCanTim)
                    {
                        indexMacDinh = i;
                        break;
                    }
                }
            }

            comboBox1_ChonThangCanXuat.SelectedIndex = indexMacDinh;
        }
        // =====================================================
        // Xuất dữ liệu (Đã tối ưu UX thông báo trên Label)
        // =====================================================
        private async void kryptonButton_XuatDuLieuSangThongKe_Click(object sender, EventArgs e)
        {
            // 1. LƯU TRẠNG THÁI GỐC CỦA NÚT
            string textBanDau = kryptonButton_XuatDuLieuSangThongKe.Values.Text;
            Image anhBanDau = kryptonButton_XuatDuLieuSangThongKe.Values.Image;

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
                    MessageBox.Show("Không tìm thấy CSDL.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 3. THIẾT LẬP TRẠNG THÁI "ĐANG XỬ LÝ"
                kryptonButton_XuatDuLieuSangThongKe.Enabled = false;
                kryptonButton_XuatDuLieuSangThongKe.Values.Text = "Đang xuất...";
                kryptonButton_XuatDuLieuSangThongKe.Values.Image = null;

                if (label3_ThongBaoThanhCong != null)
                {
                    label3_ThongBaoThanhCong.ForeColor = Color.Black;
                    label3_ThongBaoThanhCong.Text = "Đang đồng bộ dữ liệu...";
                }

                await Task.Delay(300); // Nhịp nghỉ UX tạo cảm giác "máy đang chạy"

                // 4. ĐẨY VIỆC NẶNG XUỐNG LUỒNG NGẦM (Tránh đơ Form)
                bool laTanBinh = LaPhienBanTanBinh();
                string tenBang = laTanBinh ? TABLE_TANBINH : TABLE_CBCS;
                string tenCot = GetTenCotThang(laTanBinh);
                string thangHienThi = GetTenThangHienThi();

                string thongBao = await Task.Run(() =>
                {
                    try
                    {
                        using var cn2 = new SqliteConnection($"Data Source={_csdl2Path}");
                        using var cn4 = new SqliteConnection($"Data Source={_csdl4Path}");

                        cn2.Open();
                        cn4.Open();

                        // Lấy dữ liệu 2 bên
                        var danhSach = LayDanhSachCBCS(cn2);
                        var mapID = LayMapID(cn4, tenBang);

                        using var tran = cn4.BeginTransaction();

                        foreach (var r in danhSach)
                        {
                            string phanLoai = BaoMatAES.GiaiMa(r.PhanLoaiMaHoa);
                            string giaTri = tenCot == "TongKet_Nam"
                                ? ChuyenTongKetNam(phanLoai)
                                : ChuyenLoaiSangSo(phanLoai);

                            // So khớp dựa trên thông tin ĐÃ GIẢI MÃ
                            if (mapID.TryGetValue((r.HoVaTen, r.SoHieu), out int id))
                            {
                                UpdateRecord(cn4, tran, tenBang, tenCot, id, giaTri);
                            }
                            else
                            {
                                InsertRecord(cn4, tran, tenBang, tenCot, r, giaTri);
                            }
                        }

                        if (laTanBinh)
                        {
                            // CapNhatTongLoaiTanBinh(cn4, tran);
                        }

                        tran.Commit();

                        // Trả về chuỗi chứa từ khóa "thành công" để UI nhận diện
                        return $"Xuất dữ liệu sang {thangHienThi} thành công";
                    }
                    catch (Exception exDB)
                    {
                        return "Lỗi CSDL: " + exDB.Message;
                    }
                });

                // 5. XỬ LÝ KẾT QUẢ KHI TASK HOÀN TẤT
                if (thongBao.Contains("thành công", StringComparison.OrdinalIgnoreCase))
                {
                    // THÀNH CÔNG: Cập nhật Label, êm ru không gián đoạn
                    if (label3_ThongBaoThanhCong != null)
                    {
                        label3_ThongBaoThanhCong.ForeColor = Color.DarkGreen;
                        label3_ThongBaoThanhCong.Text = $"✔ {thongBao} lúc {DateTime.Now:HH:mm:ss}";
                    }

                    NapThongKePhanLoaiTapThe(); // Chạy hàm bổ trợ
                }
                else
                {
                    // THẤT BẠI: Hiện cảnh báo trên Label và bật MessageBox
                    if (label3_ThongBaoThanhCong != null)
                    {
                        label3_ThongBaoThanhCong.ForeColor = Color.Red;
                        label3_ThongBaoThanhCong.Text = "✘ Xuất dữ liệu thất bại!";
                    }
                    MessageBox.Show(thongBao, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                if (label3_ThongBaoThanhCong != null)
                {
                    label3_ThongBaoThanhCong.ForeColor = Color.Red;
                    label3_ThongBaoThanhCong.Text = "✘ Phát sinh lỗi!";
                }
                MessageBox.Show("Lỗi chương trình: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 6. KHÔI PHỤC NÚT (Luôn chạy dù lỗi hay không)
                kryptonButton_XuatDuLieuSangThongKe.Values.Text = textBanDau;
                kryptonButton_XuatDuLieuSangThongKe.Values.Image = anhBanDau;
                kryptonButton_XuatDuLieuSangThongKe.Enabled = true;
            }
        }
        // =====================================================
        // Lấy danh sách CBCS
        // =====================================================
        private List<RecordCBCS> LayDanhSachCBCS(SqliteConnection cn)
        {
            List<RecordCBCS> list = new();

            using var cmd = new SqliteCommand("SELECT HoVaTen,SoHieu,DonVi,PhanLoai FROM DanhSach", cn);
            using var rd = cmd.ExecuteReader();

            while (rd.Read())
            {
                string rawHoTen = rd["HoVaTen"]?.ToString()?.Trim() ?? "";
                string rawSoHieu = rd["SoHieu"]?.ToString()?.Trim() ?? "";
                string rawDonVi = rd["DonVi"]?.ToString()?.Trim() ?? "";

                list.Add(new RecordCBCS
                {
                    // Lưu bản mã hóa gốc để dành cho Insert
                    HoVaTen_MaHoa = rawHoTen,
                    SoHieu_MaHoa = rawSoHieu,
                    DonVi_MaHoa = rawDonVi,

                    // Giải mã ngay tại đây để so khớp
                    HoVaTen = TryDec(rawHoTen),
                    SoHieu = TryDec(rawSoHieu),

                    PhanLoaiMaHoa = rd["PhanLoai"]?.ToString() ?? ""
                });
            }

            return list;
        }
        // =====================================================
        // Map ID để lookup nhanh (Phải giải mã để map chuẩn)
        // =====================================================
        private Dictionary<(string, string), int> LayMapID(SqliteConnection cn, string table)
        {
            var map = new Dictionary<(string, string), int>();

            using var cmd = new SqliteCommand($"SELECT ID,HoVaTen,SoHieu FROM {table}", cn);
            using var rd = cmd.ExecuteReader();

            while (rd.Read())
            {
                string decHoTen = TryDec(rd["HoVaTen"]?.ToString()?.Trim() ?? "");
                string decSoHieu = TryDec(rd["SoHieu"]?.ToString()?.Trim() ?? "");

                // Map dữ liệu dựa trên Text đã giải mã
                map[(decHoTen, decSoHieu)] = Convert.ToInt32(rd["ID"]);
            }

            return map;
        }
        // =====================================================
        // Update record
        // =====================================================
        private void UpdateRecord(SqliteConnection cn, SqliteTransaction tr,
            string table, string column, int id, string value)
        {
            using var cmd = new SqliteCommand(
                $"UPDATE {table} SET [{column}]=@v WHERE ID=@id", cn, tr);

            cmd.Parameters.AddWithValue("@v", value);
            cmd.Parameters.AddWithValue("@id", id);

            cmd.ExecuteNonQuery();
        }
        // =====================================================
        // Insert record (Dùng dữ liệu mã hóa nguyên gốc)
        // =====================================================
        private void InsertRecord(SqliteConnection cn, SqliteTransaction tr,
            string table, string column, RecordCBCS r, string value)
        {
            using var cmd = new SqliteCommand($@"
INSERT INTO {table}(HoVaTen,SoHieu,DonVi,[{column}])
VALUES(@hvt,@sh,@dv,@gt)", cn, tr);

            // Đẩy dữ liệu ĐÃ MÃ HÓA vào để bảo mật
            cmd.Parameters.AddWithValue("@hvt", r.HoVaTen_MaHoa);
            cmd.Parameters.AddWithValue("@sh", r.SoHieu_MaHoa);
            cmd.Parameters.AddWithValue("@dv", r.DonVi_MaHoa);

            cmd.Parameters.AddWithValue("@gt", value); // Điểm số lưu Plain Text

            cmd.ExecuteNonQuery();
        }
        // =====================================================
        // Chuyển loại
        // =====================================================
        private string ChuyenLoaiSangSo(string v) => v switch
        {
            "Loại 1" => "1",
            "Loại 2" => "2",
            "Loại 3" => "3",
            "Loại 4" => "4",
            _ => ""
        };
        private string ChuyenTongKetNam(string v) => v switch
        {
            "Loại 1" => "CSTĐ",
            "Loại 2" => "CSTT",
            "Loại 3" => "HTNV",
            "Loại 4" => "KHTNV",
            _ => ""
        };
        private string GetTenCotThang(bool laTanBinh)
        {
            var dict = laTanBinh ? mapTanBinh : mapCBCS;

            if (!dict.TryGetValue(comboBox1_ChonThangCanXuat.Text.Trim(), out string value))
                throw new Exception("Không xác định được cột tháng cần xuất.");

            return value;
        }
        private string GetTenThangHienThi()
        {
            if (comboBox1_ChonThangCanXuat.SelectedItem == null)
                return "";

            return comboBox1_ChonThangCanXuat.SelectedItem.ToString().Trim();
        }
        private bool LaPhienBanTanBinh()
        {
            string pb = Module_TaiKhoan.LayPhienBanPhanMem();
            return pb.Contains("tân binh", StringComparison.OrdinalIgnoreCase);
        }
        // =====================================================
        // Thống kê tập thể
        // =====================================================
        private void NapThongKePhanLoaiTapThe()
        {
            // giữ nguyên logic cũ của bạn
        }
        // =====================================================
        // Tổng loại tân binh (giữ nguyên SQL gốc)
        // =====================================================
        private void CapNhatTongLoaiTanBinh(SqliteConnection cn, SqliteTransaction tr)
        {
            string sql = @"YOUR_REAL_SQL_HERE";

            if (string.IsNullOrWhiteSpace(sql))
                return;

            using var cmd = new SqliteCommand(sql, cn, tr);
            cmd.ExecuteNonQuery();
        }
        // =====================================================
        // Cấu hình ToolTip (Gợi ý giao diện) chuẩn UX
        // =====================================================
        private void InitToolTips()
        {
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipTitle = "Gợi ý thao tác";
            toolTip1.ToolTipIcon = ToolTipIcon.Info;

            toolTip1.AutoPopDelay = 3000;
            toolTip1.InitialDelay = 400;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;

            var tips = new Dictionary<Control, string>
            {
                { comboBox1_ChonThangCanXuat, "Lựa chọn mốc thời gian (Tuần/Tháng/Năm) để đồng bộ kết quả thi đua" }
            };

            foreach (var tip in tips)
            {
                if (tip.Key != null)
                {
                    toolTip1.SetToolTip(tip.Key, tip.Value);
                }
            }
        }
        // Model dữ liệu (Đã cấu trúc lại để chứa song song 2 bản Mật/Rõ)
        class RecordCBCS
        {
            public string HoVaTen { get; set; }
            public string SoHieu { get; set; }
            public string HoVaTen_MaHoa { get; set; }
            public string SoHieu_MaHoa { get; set; }
            public string DonVi_MaHoa { get; set; }
            public string PhanLoaiMaHoa { get; set; }
        }
    }
}