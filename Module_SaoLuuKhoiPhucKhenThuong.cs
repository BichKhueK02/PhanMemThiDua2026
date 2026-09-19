using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
namespace PhanMemThiDua2026
{
    internal class Module_SaoLuuKhoiPhucKhenThuong
    {
        private readonly string _csdl4Path = Module_DanduongGPS.DuongDanCSDL4;
        private string LayGiaTriThongTin(IXLWorksheet ws, string tenTruong)
        {
            foreach (var cell in ws.Column(1).CellsUsed())
            {
                string noiDung = cell.GetString();
                if (noiDung.StartsWith(tenTruong, StringComparison.OrdinalIgnoreCase))
                {
                    int viTri = noiDung.IndexOf(':');
                    if (viTri >= 0 && viTri < noiDung.Length - 1)
                        return noiDung[(viTri + 1)..].Trim();
                }
            }
            return "Không xác định";
        }
        public void saoLuuToanBoDuLieuKhenThuong_toolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = "Tệp dữ liệu Sao lưu (*.mdf)|*.mdf|Tất cả tệp (*.*)|*.*",
                Title = "Sao lưu dữ liệu khen thưởng",
                FileName = "SaoLuu_KhenThuong_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".mdf"
            })
            {
                if (sfd.ShowDialog() != DialogResult.OK) return;
                try
                {
                    string tenMayTinh = string.IsNullOrWhiteSpace(Environment.MachineName)? "Không xác định": Environment.MachineName;
                    string userMayTinh = string.IsNullOrWhiteSpace(Environment.UserName)? "Không xác định": Environment.UserName;
                    string tenTaiKhoan = string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM)? "Không xác định": Module_TaiKhoan.TenTaiKhoan_RAM;
                    DateTime thoiGianTao = DateTime.Now;
                    // Tải dữ liệu từ CSDL vào danh sách DTO
                    var listCBCS = LayDanhSachCBCS();
                    var listGiayKhen = LayDanhSachGiayKhen();
                    var listTapThe = LayDanhSachTapThe();
                    // Tạo workbook và nạp dữ liệu
                    using (var wb = new XLWorkbook())
                    {
                        // 1. Tạo Sheet thông tin hiển thị (Excel bắt buộc phải có ít nhất 1 sheet hiển thị)
                        var wsInfo = wb.Worksheets.Add("DuLieuSaoLuu_KhenThuong");
                        wsInfo.Cell(1, 1).Value = "Tệp dữ liệu sao lưu khen thưởng";
                        wsInfo.Cell(2, 1).Value = $"Thời gian tạo: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                        wsInfo.Cell(3, 1).Value = "Ghi chú: Dữ liệu hệ thống đã được lưu trữ an toàn trong tệp này.";
                        wsInfo.Cell(4, 1).Value = "Ghi chú: Đồng chí không nên tác động vào tệp gốc - Lỗi dữ liệu.";
                        wsInfo.Cell(5, 1).Value = "Tên người dùng: " + (string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM) ? "Không xác định" : Module_TaiKhoan.TenTaiKhoan_RAM);
                        wsInfo.Cell(6, 1).Value = "Tên máy tính: " + tenMayTinh;
                        wsInfo.Cell(7, 1).Value = "User máy tính: " + userMayTinh;
                        wsInfo.Columns().AdjustToContents();
                        // 2. Tạo 3 sheet dữ liệu nghiệp vụ
                        var ws1 = wb.Worksheets.Add("ThongKeCBCS_DuocKhenThuong");
                        ws1.Cell(1, 1).InsertTable(listCBCS);
                        var ws2 = wb.Worksheets.Add("ThongKe_GiayKhen");
                        ws2.Cell(1, 1).InsertTable(listGiayKhen);
                        var ws3 = wb.Worksheets.Add("ThongKe_KhenThuongTapThe");
                        ws3.Cell(1, 1).InsertTable(listTapThe);
                        // Căn chỉnh nhanh độ rộng các cột
                        ws1.Columns().AdjustToContents();
                        ws2.Columns().AdjustToContents();
                        ws3.Columns().AdjustToContents();
                        // 3. Ẩn 3 sheet dữ liệu (Ẩn thường - Hidden)
                        ws1.Hide();
                        ws2.Hide();
                        ws3.Hide();
                        // Gọi hàm đóng dấu bản quyền phần mềm
                        Module_BanQuyen.DongDauExcel(wb);
                        // Lưu file dưới dạng Stream để bỏ qua kiểm tra đuôi tệp của ClosedXML
                        using (var stream = System.IO.File.Create(sfd.FileName))
                        {
                            wb.SaveAs(stream);
                        }
                    }
                    MessageBox.Show(
                       $"Sao lưu dữ liệu khen thưởng thành công!\n\n",
                       "Thông báo",
                       MessageBoxButtons.OK,
                       MessageBoxIcon.Information);
                    // Ghi nhật ký hệ thống
                    Module_NhatKy.GhiNhatKy(
                        taiKhoan: tenTaiKhoan,
                        hanhDong: "Sao lưu dữ liệu khen thưởng thành công ra tệp .mdf!",
                        ghiChu:
                            $"Thời gian: {thoiGianTao:dd-MM-yyyy HH:mm:ss} - " +
                            $"Máy tính: {tenMayTinh} - " +
                            $"User máy tính: {userMayTinh} - " +
                            $"Đường dẫn: {sfd.FileName}");
                    // Tự động mở cửa sổ Explorer và chọn tệp vừa tạo
                    Module_XuatNhapDuLieuThiDua.MoVaChonTepTrongExplorer(sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi sao lưu dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        public void khoiPhucDuLieuKhenThuong_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using OpenFileDialog ofd = new()
            {
                Filter = "Tệp dữ liệu Sao lưu (*.mdf)|*.mdf|Tất cả tệp (*.*)|*.*",
                Title = "Chọn file sao lưu khen thưởng (.mdf) để khôi phục dữ liệu"
            };
            if (ofd.ShowDialog() != DialogResult.OK)
                return;
            try
            {
                string tenMayTinhNguon = "Không xác định";
                string userMayTinhNguon = "Không xác định";
                string tenNguoiDungNguon = "Không xác định";
                string thoiGianTao = "Không xác định";
                using var stream = File.OpenRead(ofd.FileName);
                using var wb = new XLWorkbook(stream);
                // Kiểm tra cấu trúc
                string[] sheetBatBuoc =
                {
            "ThongKeCBCS_DuocKhenThuong",
            "ThongKe_GiayKhen",
            "ThongKe_KhenThuongTapThe"
        };
                if (sheetBatBuoc.Any(sheet => !wb.Worksheets.Contains(sheet)))
                {
                    MessageBox.Show(
                        "Tệp dữ liệu không đúng cấu trúc!\n\n" +
                        "Cần có đủ 3 sheet:\n" +
                        string.Join("\n", sheetBatBuoc),
                        "Lỗi cấu trúc",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                // Đọc thông tin nguồn tạo tệp
                if (wb.Worksheets.Contains("DuLieuSaoLuu_KhenThuong"))
                {
                    var wsInfo = wb.Worksheet("DuLieuSaoLuu_KhenThuong");
                    thoiGianTao = LayGiaTriThongTin(
                        wsInfo,
                        "Thời gian xuất bản:");
                    tenNguoiDungNguon = LayGiaTriThongTin(
                        wsInfo,
                        "Tên người dùng:");
                    tenMayTinhNguon = LayGiaTriThongTin(
                        wsInfo,
                        "Tên máy tính:");
                    userMayTinhNguon = LayGiaTriThongTin(
                        wsInfo,
                        "User máy tính:");
                }
                // Xác nhận phương thức khôi phục
                string thongBao =
                    "THÔNG TIN TỆP SAO LƯU\n\n" +
                    $"Thời gian xuất bản: {thoiGianTao}\n" +
                    $"Tên người dùng: {tenNguoiDungNguon}\n" +
                    $"Tên máy tính: {tenMayTinhNguon}\n" +
                    $"User máy tính: {userMayTinhNguon}\n\n" +
                    "PHƯƠNG THỨC KHÔI PHỤC\n\n" +
                    "[Yes]  XÓA SẠCH dữ liệu cũ và nạp mới.\n" +
                    "[No]   GIỮ dữ liệu cũ, thêm mới và cập nhật.\n" +
                    "[Cancel] Hủy thao tác.\n\n" +
                    "Bạn có muốn tiếp tục khôi phục dữ liệu?";
                DialogResult confirm = MessageBox.Show(
                    thongBao,
                    "Khôi phục dữ liệu khen thưởng",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);
                if (confirm == DialogResult.Cancel)
                    return;
                bool isClearData = confirm == DialogResult.Yes;
                // Đọc dữ liệu từ 3 sheet
                var listCBCS = DocSheetCBCS(
                    wb.Worksheet("ThongKeCBCS_DuocKhenThuong"));
                var listGiayKhen = DocSheetGiayKhen(
                    wb.Worksheet("ThongKe_GiayKhen"));
                var listTapThe = DocSheetTapThe(
                    wb.Worksheet("ThongKe_KhenThuongTapThe"));
                // Lưu vào CSDL
                LuuVaoDatabase(
                    listCBCS,
                    listGiayKhen,
                    listTapThe,
                    isClearData);
                // Thông báo thành công
                MessageBox.Show(
                    $"Khôi phục dữ liệu khen thưởng thành công!\n\n" +
                    $"Tệp được tạo từ máy tính: {tenMayTinhNguon}\n" +
                    $"User máy tính: {userMayTinhNguon}",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                // Ghi nhật ký
                Module_NhatKy.GhiNhatKy(
                    taiKhoan:
                        string.IsNullOrWhiteSpace(Module_TaiKhoan.TenTaiKhoan_RAM)
                            ? "Không xác định"
                            : Module_TaiKhoan.TenTaiKhoan_RAM,
                    hanhDong:
                        "Khôi phục dữ liệu khen thưởng từ tệp .mdf thành công!",
                    ghiChu:
                        $"Thời gian: {DateTime.Now:dd-MM-yyyy HH:mm:ss} - " +
                        $"Nguồn máy tính: {tenMayTinhNguon} - " +
                        $"Nguồn User: {userMayTinhNguon} - " +
                        $"Tệp: {ofd.FileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Lỗi khi khôi phục dữ liệu: " + ex.Message,
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void LuuVaoDatabase(List<CBCSDuocKhenThuongDTO> listCBCS, List<GiayKhenDTO> listGiayKhen, List<KhenThuongTapTheDTO> listTapThe, bool isClearData)
        {
            using (var conn = new SqliteConnection($"Data Source={_csdl4Path}"))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Chỉ xóa sạch dữ liệu nếu người dùng chọn Yes
                        if (isClearData)
                        {
                            using (var cmdClear = conn.CreateCommand())
                            {
                                cmdClear.Transaction = trans;
                                cmdClear.CommandText = @"
                                    DELETE FROM ThongKeCBCS_DuocKhenThuong;
                                    DELETE FROM ThongKe_GiayKhen;
                                    DELETE FROM ThongKe_KhenThuongTapThe;
                                    DELETE FROM sqlite_sequence WHERE name IN ('ThongKeCBCS_DuocKhenThuong', 'ThongKe_GiayKhen', 'ThongKe_KhenThuongTapThe');
                                ";
                                cmdClear.ExecuteNonQuery();
                            }
                        }
                        // 2. Chèn dữ liệu ThongKeCBCS_DuocKhenThuong
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = trans;
                            cmd.CommandText = @"INSERT OR REPLACE INTO ThongKeCBCS_DuocKhenThuong (ID, HoVaTen, SoHieu, DonVi, TinhTrang, SoLuong_Khen, GhiChu_Khen) 
                                                VALUES (@ID, @HoVaTen, @SoHieu, @DonVi, @TinhTrang, @SoLuong_Khen, @GhiChu_Khen);";
                            cmd.Parameters.Add("@ID", SqliteType.Integer);
                            cmd.Parameters.Add("@HoVaTen", SqliteType.Text);
                            cmd.Parameters.Add("@SoHieu", SqliteType.Text);
                            cmd.Parameters.Add("@DonVi", SqliteType.Text);
                            cmd.Parameters.Add("@TinhTrang", SqliteType.Text);
                            cmd.Parameters.Add("@SoLuong_Khen", SqliteType.Text);
                            cmd.Parameters.Add("@GhiChu_Khen", SqliteType.Text);
                            foreach (var item in listCBCS)
                            {
                                cmd.Parameters["@ID"].Value = item.ID == 0 ? DBNull.Value : (object)item.ID;
                                cmd.Parameters["@HoVaTen"].Value = (object)item.HoVaTen ?? DBNull.Value;
                                cmd.Parameters["@SoHieu"].Value = (object)item.SoHieu ?? DBNull.Value;
                                cmd.Parameters["@DonVi"].Value = (object)item.DonVi ?? DBNull.Value;
                                cmd.Parameters["@TinhTrang"].Value = (object)item.TinhTrang ?? DBNull.Value;
                                cmd.Parameters["@SoLuong_Khen"].Value = (object)item.SoLuong_Khen ?? DBNull.Value;
                                cmd.Parameters["@GhiChu_Khen"].Value = (object)item.GhiChu_Khen ?? DBNull.Value;
                                cmd.ExecuteNonQuery();
                            }
                        }
                        // 3. Chèn dữ liệu ThongKe_GiayKhen
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = trans;
                            cmd.CommandText = @"INSERT OR REPLACE INTO ThongKe_GiayKhen (ID, HoVaTen, SoHieu, DonVi, TinhTrang, HinhThuc_Khen, QuyetDinh_Khen, NgayCapQD_Khen, DonVi_Khen, VeViec_Khen, TienThuong, NgayCapPhat, CanBoCapPhat, GhiChu_Khen) 
                                                VALUES (@ID, @HoVaTen, @SoHieu, @DonVi, @TinhTrang, @HinhThuc_Khen, @QuyetDinh_Khen, @NgayCapQD_Khen, @DonVi_Khen, @VeViec_Khen, @TienThuong, @NgayCapPhat, @CanBoCapPhat, @GhiChu_Khen);";
                            cmd.Parameters.Add("@ID", SqliteType.Integer);
                            cmd.Parameters.Add("@HoVaTen", SqliteType.Text);
                            cmd.Parameters.Add("@SoHieu", SqliteType.Text);
                            cmd.Parameters.Add("@DonVi", SqliteType.Text);
                            cmd.Parameters.Add("@TinhTrang", SqliteType.Text);
                            cmd.Parameters.Add("@HinhThuc_Khen", SqliteType.Text);
                            cmd.Parameters.Add("@QuyetDinh_Khen", SqliteType.Text);
                            cmd.Parameters.Add("@NgayCapQD_Khen", SqliteType.Text);
                            cmd.Parameters.Add("@DonVi_Khen", SqliteType.Text);
                            cmd.Parameters.Add("@VeViec_Khen", SqliteType.Text);
                            cmd.Parameters.Add("@TienThuong", SqliteType.Text);
                            cmd.Parameters.Add("@NgayCapPhat", SqliteType.Text);
                            cmd.Parameters.Add("@CanBoCapPhat", SqliteType.Text);
                            cmd.Parameters.Add("@GhiChu_Khen", SqliteType.Text);
                            foreach (var item in listGiayKhen)
                            {
                                cmd.Parameters["@ID"].Value = item.ID == 0 ? DBNull.Value : (object)item.ID;
                                cmd.Parameters["@HoVaTen"].Value = (object)item.HoVaTen ?? DBNull.Value;
                                cmd.Parameters["@SoHieu"].Value = (object)item.SoHieu ?? DBNull.Value;
                                cmd.Parameters["@DonVi"].Value = (object)item.DonVi ?? DBNull.Value;
                                cmd.Parameters["@TinhTrang"].Value = (object)item.TinhTrang ?? DBNull.Value;
                                cmd.Parameters["@HinhThuc_Khen"].Value = (object)item.HinhThuc_Khen ?? DBNull.Value;
                                cmd.Parameters["@QuyetDinh_Khen"].Value = (object)item.QuyetDinh_Khen ?? DBNull.Value;
                                cmd.Parameters["@NgayCapQD_Khen"].Value = (object)item.NgayCapQD_Khen ?? DBNull.Value;
                                cmd.Parameters["@DonVi_Khen"].Value = (object)item.DonVi_Khen ?? DBNull.Value;
                                cmd.Parameters["@VeViec_Khen"].Value = (object)item.VeViec_Khen ?? DBNull.Value;
                                cmd.Parameters["@TienThuong"].Value = (object)item.TienThuong ?? DBNull.Value;
                                cmd.Parameters["@NgayCapPhat"].Value = (object)item.NgayCapPhat ?? DBNull.Value;
                                cmd.Parameters["@CanBoCapPhat"].Value = (object)item.CanBoCapPhat ?? DBNull.Value;
                                cmd.Parameters["@GhiChu_Khen"].Value = (object)item.GhiChu_Khen ?? DBNull.Value;
                                cmd.ExecuteNonQuery();
                            }
                        }
                        // 4. Chèn dữ liệu ThongKe_KhenThuongTapThe
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = trans;
                            cmd.CommandText = @"INSERT OR REPLACE INTO ThongKe_KhenThuongTapThe (ID, STT, TenTapThe, HinhThuc_KhenThuong, DonVi_CapKhenThuong, SoQuyetDinh, NgayQuyetDinh, NguoiKy, NoiDung_KhenThuong, TienThuong, NgayCapPhat, CanBoCapPhat, NguoiDaiDienNhan, GhiChu) 
                                                VALUES (@ID, @STT, @TenTapThe, @HinhThuc_KhenThuong, @DonVi_CapKhenThuong, @SoQuyetDinh, @NgayQuyetDinh, @NguoiKy, @NoiDung_KhenThuong, @TienThuong, @NgayCapPhat, @CanBoCapPhat, @NguoiDaiDienNhan, @GhiChu);";
                            cmd.Parameters.Add("@ID", SqliteType.Integer);
                            cmd.Parameters.Add("@STT", SqliteType.Integer);
                            cmd.Parameters.Add("@TenTapThe", SqliteType.Text);
                            cmd.Parameters.Add("@HinhThuc_KhenThuong", SqliteType.Text);
                            cmd.Parameters.Add("@DonVi_CapKhenThuong", SqliteType.Text);
                            cmd.Parameters.Add("@SoQuyetDinh", SqliteType.Text);
                            cmd.Parameters.Add("@NgayQuyetDinh", SqliteType.Text);
                            cmd.Parameters.Add("@NguoiKy", SqliteType.Text);
                            cmd.Parameters.Add("@NoiDung_KhenThuong", SqliteType.Text);
                            cmd.Parameters.Add("@TienThuong", SqliteType.Text);
                            cmd.Parameters.Add("@NgayCapPhat", SqliteType.Text);
                            cmd.Parameters.Add("@CanBoCapPhat", SqliteType.Text);
                            cmd.Parameters.Add("@NguoiDaiDienNhan", SqliteType.Text);
                            cmd.Parameters.Add("@GhiChu", SqliteType.Text);
                            foreach (var item in listTapThe)
                            {
                                cmd.Parameters["@ID"].Value = item.ID == 0 ? DBNull.Value : (object)item.ID;
                                cmd.Parameters["@STT"].Value = item.STT;
                                cmd.Parameters["@TenTapThe"].Value = (object)item.TenTapThe ?? DBNull.Value;
                                cmd.Parameters["@HinhThuc_KhenThuong"].Value = (object)item.HinhThuc_KhenThuong ?? DBNull.Value;
                                cmd.Parameters["@DonVi_CapKhenThuong"].Value = (object)item.DonVi_CapKhenThuong ?? DBNull.Value;
                                cmd.Parameters["@SoQuyetDinh"].Value = (object)item.SoQuyetDinh ?? DBNull.Value;
                                cmd.Parameters["@NgayQuyetDinh"].Value = (object)item.NgayQuyetDinh ?? DBNull.Value;
                                cmd.Parameters["@NguoiKy"].Value = (object)item.NguoiKy ?? DBNull.Value;
                                cmd.Parameters["@NoiDung_KhenThuong"].Value = (object)item.NoiDung_KhenThuong ?? DBNull.Value;
                                cmd.Parameters["@TienThuong"].Value = (object)item.TienThuong ?? DBNull.Value;
                                cmd.Parameters["@NgayCapPhat"].Value = (object)item.NgayCapPhat ?? DBNull.Value;
                                cmd.Parameters["@CanBoCapPhat"].Value = (object)item.CanBoCapPhat ?? DBNull.Value;
                                cmd.Parameters["@NguoiDaiDienNhan"].Value = (object)item.NguoiDaiDienNhan ?? DBNull.Value;
                                cmd.Parameters["@GhiChu"].Value = (object)item.GhiChu ?? DBNull.Value;
                                cmd.ExecuteNonQuery();
                            }
                        }
                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }
        private List<CBCSDuocKhenThuongDTO> LayDanhSachCBCS()
        {
            var list = new List<CBCSDuocKhenThuongDTO>();
            using (var conn = new SqliteConnection($"Data Source={_csdl4Path}"))
            {
                conn.Open();
                using (var cmd = new SqliteCommand("SELECT ID, HoVaTen, SoHieu, DonVi, TinhTrang, SoLuong_Khen, GhiChu_Khen FROM ThongKeCBCS_DuocKhenThuong", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new CBCSDuocKhenThuongDTO
                        {
                            ID = dr.IsDBNull(0) ? 0 : dr.GetInt64(0),
                            HoVaTen = dr.IsDBNull(1) ? null : dr.GetString(1),
                            SoHieu = dr.IsDBNull(2) ? null : dr.GetString(2),
                            DonVi = dr.IsDBNull(3) ? null : dr.GetString(3),
                            TinhTrang = dr.IsDBNull(4) ? null : dr.GetString(4),
                            SoLuong_Khen = dr.IsDBNull(5) ? null : dr.GetString(5),
                            GhiChu_Khen = dr.IsDBNull(6) ? null : dr.GetString(6)
                        });
                    }
                }
            }
            return list;
        }
        private List<GiayKhenDTO> LayDanhSachGiayKhen()
        {
            var list = new List<GiayKhenDTO>();
            using (var conn = new SqliteConnection($"Data Source={_csdl4Path}"))
            {
                conn.Open();
                using (var cmd = new SqliteCommand("SELECT ID, HoVaTen, SoHieu, DonVi, TinhTrang, HinhThuc_Khen, QuyetDinh_Khen, NgayCapQD_Khen, DonVi_Khen, VeViec_Khen, TienThuong, NgayCapPhat, CanBoCapPhat, GhiChu_Khen FROM ThongKe_GiayKhen", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new GiayKhenDTO
                        {
                            ID = dr.IsDBNull(0) ? 0 : dr.GetInt64(0),
                            HoVaTen = dr.IsDBNull(1) ? null : dr.GetString(1),
                            SoHieu = dr.IsDBNull(2) ? null : dr.GetString(2),
                            DonVi = dr.IsDBNull(3) ? null : dr.GetString(3),
                            TinhTrang = dr.IsDBNull(4) ? null : dr.GetString(4),
                            HinhThuc_Khen = dr.IsDBNull(5) ? null : dr.GetString(5),
                            QuyetDinh_Khen = dr.IsDBNull(6) ? null : dr.GetString(6),
                            NgayCapQD_Khen = dr.IsDBNull(7) ? null : dr.GetString(7),
                            DonVi_Khen = dr.IsDBNull(8) ? null : dr.GetString(8),
                            VeViec_Khen = dr.IsDBNull(9) ? null : dr.GetString(9),
                            TienThuong = dr.IsDBNull(10) ? null : dr.GetString(10),
                            NgayCapPhat = dr.IsDBNull(11) ? null : dr.GetString(11),
                            CanBoCapPhat = dr.IsDBNull(12) ? null : dr.GetString(12),
                            GhiChu_Khen = dr.IsDBNull(13) ? null : dr.GetString(13)
                        });
                    }
                }
            }
            return list;
        }
        private List<KhenThuongTapTheDTO> LayDanhSachTapThe()
        {
            var list = new List<KhenThuongTapTheDTO>();
            using (var conn = new SqliteConnection($"Data Source={_csdl4Path}"))
            {
                conn.Open();
                using (var cmd = new SqliteCommand("SELECT ID, STT, TenTapThe, HinhThuc_KhenThuong, DonVi_CapKhenThuong, SoQuyetDinh, NgayQuyetDinh, NguoiKy, NoiDung_KhenThuong, TienThuong, NgayCapPhat, CanBoCapPhat, NguoiDaiDienNhan, GhiChu FROM ThongKe_KhenThuongTapThe", conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new KhenThuongTapTheDTO
                        {
                            ID = dr.IsDBNull(0) ? 0 : dr.GetInt64(0),
                            STT = dr.IsDBNull(1) ? 0 : dr.GetInt64(1),
                            TenTapThe = dr.IsDBNull(2) ? null : dr.GetString(2),
                            HinhThuc_KhenThuong = dr.IsDBNull(3) ? null : dr.GetString(3),
                            DonVi_CapKhenThuong = dr.IsDBNull(4) ? null : dr.GetString(4),
                            SoQuyetDinh = dr.IsDBNull(5) ? null : dr.GetString(5),
                            NgayQuyetDinh = dr.IsDBNull(6) ? null : dr.GetString(6),
                            NguoiKy = dr.IsDBNull(7) ? null : dr.GetString(7),
                            NoiDung_KhenThuong = dr.IsDBNull(8) ? null : dr.GetString(8),
                            TienThuong = dr.IsDBNull(9) ? null : dr.GetString(9),
                            NgayCapPhat = dr.IsDBNull(10) ? null : dr.GetString(10),
                            CanBoCapPhat = dr.IsDBNull(11) ? null : dr.GetString(11),
                            NguoiDaiDienNhan = dr.IsDBNull(12) ? null : dr.GetString(12),
                            GhiChu = dr.IsDBNull(13) ? null : dr.GetString(13)
                        });
                    }
                }
            }
            return list;
        }
        private List<CBCSDuocKhenThuongDTO> DocSheetCBCS(IXLWorksheet ws)
        {
            var list = new List<CBCSDuocKhenThuongDTO>();
            var rows = ws.RowsUsed().Skip(1);
            foreach (var row in rows)
            {
                long.TryParse(row.Cell(1).GetValue<string>(), out long id);
                list.Add(new CBCSDuocKhenThuongDTO
                {
                    ID = id,
                    HoVaTen = row.Cell(2).GetString(),
                    SoHieu = row.Cell(3).GetString(),
                    DonVi = row.Cell(4).GetString(),
                    TinhTrang = row.Cell(5).GetString(),
                    SoLuong_Khen = row.Cell(6).GetString(),
                    GhiChu_Khen = row.Cell(7).GetString()
                });
            }
            return list;
        }
        private List<GiayKhenDTO> DocSheetGiayKhen(IXLWorksheet ws)
        {
            var list = new List<GiayKhenDTO>();
            var rows = ws.RowsUsed().Skip(1);
            foreach (var row in rows)
            {
                long.TryParse(row.Cell(1).GetValue<string>(), out long id);
                list.Add(new GiayKhenDTO
                {
                    ID = id,
                    HoVaTen = row.Cell(2).GetString(),
                    SoHieu = row.Cell(3).GetString(),
                    DonVi = row.Cell(4).GetString(),
                    TinhTrang = row.Cell(5).GetString(),
                    HinhThuc_Khen = row.Cell(6).GetString(),
                    QuyetDinh_Khen = row.Cell(7).GetString(),
                    NgayCapQD_Khen = row.Cell(8).GetString(),
                    DonVi_Khen = row.Cell(9).GetString(),
                    VeViec_Khen = row.Cell(10).GetString(),
                    TienThuong = row.Cell(11).GetString(),
                    NgayCapPhat = row.Cell(12).GetString(),
                    CanBoCapPhat = row.Cell(13).GetString(),
                    GhiChu_Khen = row.Cell(14).GetString()
                });
            }
            return list;
        }
        private List<KhenThuongTapTheDTO> DocSheetTapThe(IXLWorksheet ws)
        {
            var list = new List<KhenThuongTapTheDTO>();
            var rows = ws.RowsUsed().Skip(1);
            foreach (var row in rows)
            {
                long.TryParse(row.Cell(1).GetValue<string>(), out long id);
                long.TryParse(row.Cell(2).GetValue<string>(), out long stt);
                list.Add(new KhenThuongTapTheDTO
                {
                    ID = id,
                    STT = stt,
                    TenTapThe = row.Cell(3).GetString(),
                    HinhThuc_KhenThuong = row.Cell(4).GetString(),
                    DonVi_CapKhenThuong = row.Cell(5).GetString(),
                    SoQuyetDinh = row.Cell(6).GetString(),
                    NgayQuyetDinh = row.Cell(7).GetString(),
                    NguoiKy = row.Cell(8).GetString(),
                    NoiDung_KhenThuong = row.Cell(9).GetString(),
                    TienThuong = row.Cell(10).GetString(),
                    NgayCapPhat = row.Cell(11).GetString(),
                    CanBoCapPhat = row.Cell(12).GetString(),
                    NguoiDaiDienNhan = row.Cell(13).GetString(),
                    GhiChu = row.Cell(14).GetString()
                });
            }
            return list;
        }
    }
    public class CBCSDuocKhenThuongDTO
    {
        public long ID { get; set; }
        public string HoVaTen { get; set; }
        public string SoHieu { get; set; }
        public string DonVi { get; set; }
        public string TinhTrang { get; set; }
        public string SoLuong_Khen { get; set; }
        public string GhiChu_Khen { get; set; }
    }
    public class GiayKhenDTO
    {
        public long ID { get; set; }
        public string HoVaTen { get; set; }
        public string SoHieu { get; set; }
        public string DonVi { get; set; }
        public string TinhTrang { get; set; }
        public string HinhThuc_Khen { get; set; }
        public string QuyetDinh_Khen { get; set; }
        public string NgayCapQD_Khen { get; set; }
        public string DonVi_Khen { get; set; }
        public string VeViec_Khen { get; set; }
        public string TienThuong { get; set; }
        public string NgayCapPhat { get; set; }
        public string CanBoCapPhat { get; set; }
        public string GhiChu_Khen { get; set; }
    }
    public class KhenThuongTapTheDTO
    {
        public long ID { get; set; }
        public long STT { get; set; }
        public string TenTapThe { get; set; }
        public string HinhThuc_KhenThuong { get; set; }
        public string DonVi_CapKhenThuong { get; set; }
        public string SoQuyetDinh { get; set; }
        public string NgayQuyetDinh { get; set; }
        public string NguoiKy { get; set; }
        public string NoiDung_KhenThuong { get; set; }
        public string TienThuong { get; set; }
        public string NgayCapPhat { get; set; }
        public string CanBoCapPhat { get; set; }
        public string NguoiDaiDienNhan { get; set; }
        public string GhiChu { get; set; }
    }
}