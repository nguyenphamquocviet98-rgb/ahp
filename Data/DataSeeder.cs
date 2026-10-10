using Microsoft.EntityFrameworkCore;
using code.Models;
using code.Services;

namespace code.Data
{
    public static class DataSeeder
    {
        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var ahpService = scope.ServiceProvider.GetRequiredService<AHPService>();

            // Tự động tạo Database và cấu trúc bảng nếu chưa tồn tại
            try
            {
                await context.Database.EnsureCreatedAsync();
            }
            catch
            {
                // Bỏ qua nếu DB đã được tạo thủ công hoặc dùng quyền giới hạn
            }

            // 0. Đảm bảo cấu trúc cột mới cho SanPham và NhaCungCap
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    IF OBJECT_ID('SanPham', 'U') IS NOT NULL AND COL_LENGTH('SanPham', 'AnhSanPham') IS NULL
                    BEGIN
                        ALTER TABLE SanPham ADD AnhSanPham NVARCHAR(255) NULL;
                    END

                    IF OBJECT_ID('NhaCungCap', 'U') IS NOT NULL AND COL_LENGTH('NhaCungCap', 'HinhAnh') IS NULL
                    BEGIN
                        ALTER TABLE NhaCungCap ADD HinhAnh NVARCHAR(255) NULL;
                    END
                ");
            }
            catch
            {
                // Bỏ qua nếu môi trường DB không hỗ trợ cú pháp này
            }

            // 1. Seed Người dùng mẫu
            try
            {
                if (!await context.NguoiDungs.AnyAsync())
                {
                    var users = new List<NguoiDung>
                    {
                        new NguoiDung { TenDangNhap = "admin", MatKhau = "123456", VaiTro = "admin", TrangThai = true, NgayTao = DateTime.Now.AddMonths(-6) },
                        new NguoiDung { TenDangNhap = "quanly", MatKhau = "123456", VaiTro = "quanly", TrangThai = true, NgayTao = DateTime.Now.AddMonths(-6) },
                        new NguoiDung { TenDangNhap = "nhanvien", MatKhau = "123456", VaiTro = "nhanvien", TrangThai = true, NgayTao = DateTime.Now.AddMonths(-6) }
                    };
                    var passwords = scope.ServiceProvider.GetRequiredService<PasswordService>();
                    foreach (var user in users) user.MatKhau = passwords.Hash(user, user.MatKhau!);
                    context.NguoiDungs.AddRange(users);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex) { Console.WriteLine($"[Seed Users]: {ex.Message}"); }

            // 2. Seed Tiêu chí AHP
            try
            {
                if (!await context.TieuChis.AnyAsync())
                {
                    var tieuChis = new List<TieuChi>
                    {
                        new TieuChi { TenTieuChi = "Giá cả & Chiết khấu", LoaiTieuChi = "cost" },
                        new TieuChi { TenTieuChi = "Chất lượng hàng hóa", LoaiTieuChi = "benefit" },
                        new TieuChi { TenTieuChi = "Thời gian giao hàng", LoaiTieuChi = "cost" },
                        new TieuChi { TenTieuChi = "Độ uy tín & Dịch vụ", LoaiTieuChi = "benefit" }
                    };
                    context.TieuChis.AddRange(tieuChis);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex) { Console.WriteLine($"[Seed TieuChi]: {ex.Message}"); }

            // 3. Seed Danh mục hàng hóa phong phú
            try
            {
                var existingDmCount = await context.DanhMucs.CountAsync();
                if (existingDmCount < 6)
                {
                    var danhMucs = new List<DanhMuc>
                    {
                        new DanhMuc { TenDanhMuc = "Bánh kẹo & Đồ ngọt", MoTa = "Các loại bánh quy, kẹo dẻo, socola nhập khẩu và nội địa" },
                        new DanhMuc { TenDanhMuc = "Đồ uống & Giải khát", MoTa = "Nước ngọt có gas, trà xanh, nước khoáng thiên nhiên, bia" },
                        new DanhMuc { TenDanhMuc = "Gia vị & Thực phẩm khô", MoTa = "Dầu ăn, nước mắm, tương ớt, mì gói, bún miến phở khô" },
                        new DanhMuc { TenDanhMuc = "Sữa & Chế phẩm từ sữa", MoTa = "Sữa tươi tiệt trùng, sữa chua ăn/uống, bơ, phô mai" },
                        new DanhMuc { TenDanhMuc = "Đồ hộp & Chế biến sẵn", MoTa = "Cá hộp, pate, thịt nguội, xúc xích tiệt trùng" },
                        new DanhMuc { TenDanhMuc = "Hóa mỹ phẩm & Tẩy rửa", MoTa = "Nước giặt, xả vải, nước rửa chén, dầu gội, xà phòng" }
                    };

                    foreach (var dm in danhMucs)
                    {
                        if (!await context.DanhMucs.AnyAsync(d => d.TenDanhMuc == dm.TenDanhMuc))
                        {
                            context.DanhMucs.Add(dm);
                        }
                    }
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex) { Console.WriteLine($"[Seed DanhMuc]: {ex.Message}"); }

            // 4. Seed Đơn vị tính
            try
            {
                var existingDvtCount = await context.DonViTinhs.CountAsync();
                if (existingDvtCount < 7)
                {
                    var dvts = new List<DonViTinh>
                    {
                        new DonViTinh { TenDonVi = "Thùng" },
                        new DonViTinh { TenDonVi = "Hộp" },
                        new DonViTinh { TenDonVi = "Chai" },
                        new DonViTinh { TenDonVi = "Gói" },
                        new DonViTinh { TenDonVi = "Kg" },
                        new DonViTinh { TenDonVi = "Lon" },
                        new DonViTinh { TenDonVi = "Lốc" },
                        new DonViTinh { TenDonVi = "Can" }
                    };

                    foreach (var dvt in dvts)
                    {
                        if (!await context.DonViTinhs.AnyAsync(d => d.TenDonVi == dvt.TenDonVi))
                        {
                            context.DonViTinhs.Add(dvt);
                        }
                    }
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex) { Console.WriteLine($"[Seed DonViTinh]: {ex.Message}"); }

            // 5. Seed Danh sách Nhà cung cấp chuyên nghiệp (8 đối tác lớn)
            try
            {
                var existingNccCount = await context.NhaCungCaps.CountAsync();
                if (existingNccCount < 8)
                {
                    var sampleNCCs = new List<NhaCungCap>
                    {
                        new NhaCungCap 
                        { 
                            MaNhaCungCapCode = "NCC001", 
                            TenNhaCungCap = "Công ty TNHH Thực phẩm Á Châu (Asia Foods)", 
                            NguoiLienHe = "Nguyễn Văn Tuấn", 
                            SoDienThoai = "0912345678", 
                            Email = "tuan.nguyen@asiafoods.vn", 
                            DiaChi = "120 Nguyễn Huệ, Bến Nghé, Quận 1, TP.HCM", 
                            HinhAnh = "https://images.unsplash.com/photo-1542838132-92c53300491e?w=200&auto=format&fit=crop&q=60",
                            TrangThai = true 
                        },
                        new NhaCungCap 
                        { 
                            MaNhaCungCapCode = "NCC002", 
                            TenNhaCungCap = "Công ty CP Tập đoàn Masan Consumer", 
                            NguoiLienHe = "Phạm Thị Hương", 
                            SoDienThoai = "0987654321", 
                            Email = "huong.pham@masangroup.com", 
                            DiaChi = "Tòa nhà Mplaza, 39 Lê Duẩn, Quận 1, TP.HCM", 
                            HinhAnh = "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?w=200&auto=format&fit=crop&q=60",
                            TrangThai = true 
                        },
                        new NhaCungCap 
                        { 
                            MaNhaCungCapCode = "NCC003", 
                            TenNhaCungCap = "Công ty Cổ phần Sữa Việt Nam (Vinamilk)", 
                            NguoiLienHe = "Trần Minh Đăng", 
                            SoDienThoai = "0908112233", 
                            Email = "dang.tm@vinamilk.com.vn", 
                            DiaChi = "10 Tân Trào, Tân Phú, Quận 7, TP.HCM", 
                            HinhAnh = "https://images.unsplash.com/photo-1528732263440-4dd1a18a4cc2?w=200&auto=format&fit=crop&q=60",
                            TrangThai = true 
                        },
                        new NhaCungCap 
                        { 
                            MaNhaCungCapCode = "NCC004", 
                            TenNhaCungCap = "Tập đoàn Nước giải khát Tân Hiệp Phát", 
                            NguoiLienHe = "Lê Hoàng Quân", 
                            SoDienThoai = "0933445566", 
                            Email = "quan.le@thp.com.vn", 
                            DiaChi = "219 Đại lộ Bình Dương, Thuận An, Bình Dương", 
                            HinhAnh = "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=200&auto=format&fit=crop&q=60",
                            TrangThai = true 
                        },
                        new NhaCungCap 
                        { 
                            MaNhaCungCapCode = "NCC005", 
                            TenNhaCungCap = "Công ty TNHH Nestlé Việt Nam", 
                            NguoiLienHe = "Hoàng Mai Linh", 
                            SoDienThoai = "0945678901", 
                            Email = "linh.hoang@vn.nestle.com", 
                            DiaChi = "Lầu 5, Empress Tower, 138-142 Hai Bà Trưng, Quận 1, TP.HCM", 
                            HinhAnh = "https://images.unsplash.com/photo-1550583724-b2692b85b150?w=200&auto=format&fit=crop&q=60",
                            TrangThai = true 
                        },
                        new NhaCungCap 
                        { 
                            MaNhaCungCapCode = "NCC006", 
                            TenNhaCungCap = "Công ty TNHH Dầu thực vật Cái Lân (CALOFIC)", 
                            NguoiLienHe = "Vũ Đình Trọng", 
                            SoDienThoai = "0978123456", 
                            Email = "trong.vu@calofic.com.vn", 
                            DiaChi = "Khu công nghiệp Cái Lân, TP. Hạ Long, Quảng Ninh", 
                            HinhAnh = "https://images.unsplash.com/photo-1471193945509-9ad0617afabf?w=200&auto=format&fit=crop&q=60",
                            TrangThai = true 
                        },
                        new NhaCungCap 
                        { 
                            MaNhaCungCapCode = "NCC007", 
                            TenNhaCungCap = "Công ty Cổ phần Bibica", 
                            NguoiLienHe = "Đỗ Bích Ngọc", 
                            SoDienThoai = "0967891234", 
                            Email = "ngoc.do@bibica.com.vn", 
                            DiaChi = "443 Lý Thường Kiệt, Phường 8, Quận Tân Bình, TP.HCM", 
                            HinhAnh = "https://images.unsplash.com/photo-1578985545062-69928b1d9587?w=200&auto=format&fit=crop&q=60",
                            TrangThai = true 
                        },
                        new NhaCungCap 
                        { 
                            MaNhaCungCapCode = "NCC008", 
                            TenNhaCungCap = "Công ty TNHH Quốc tế Unilever Việt Nam", 
                            NguoiLienHe = "Bùi Anh Tuấn", 
                            SoDienThoai = "0918765432", 
                            Email = "tuan.bui@unilever.com", 
                            DiaChi = "A2-3 KCN Tây Bắc Củ Chi, Huyện Củ Chi, TP.HCM", 
                            HinhAnh = "https://images.unsplash.com/photo-1583947215259-38e31be8751f?w=200&auto=format&fit=crop&q=60",
                            TrangThai = true 
                        }
                    };

                    foreach (var ncc in sampleNCCs)
                    {
                        if (!await context.NhaCungCaps.AnyAsync(n => n.MaNhaCungCapCode == ncc.MaNhaCungCapCode))
                        {
                            context.NhaCungCaps.Add(ncc);
                        }
                    }
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex) { Console.WriteLine($"[Seed NhaCungCap]: {ex.Message}"); }

            // 6. Seed Danh mục 20+ Sản phẩm phong phú kèm hình ảnh, giá và tồn kho
            try
            {
                var existingSpCount = await context.SanPhams.CountAsync();
                if (existingSpCount < 18)
                {
                    var dmBanhKeo = await context.DanhMucs.FirstOrDefaultAsync(d => d.TenDanhMuc != null && d.TenDanhMuc.Contains("Bánh kẹo")) ?? await context.DanhMucs.FirstAsync();
                    var dmDoUong = await context.DanhMucs.FirstOrDefaultAsync(d => d.TenDanhMuc != null && d.TenDanhMuc.Contains("Đồ uống")) ?? dmBanhKeo;
                    var dmGiaVi = await context.DanhMucs.FirstOrDefaultAsync(d => d.TenDanhMuc != null && d.TenDanhMuc.Contains("Gia vị")) ?? dmBanhKeo;
                    var dmSua = await context.DanhMucs.FirstOrDefaultAsync(d => d.TenDanhMuc != null && d.TenDanhMuc.Contains("Sữa")) ?? dmBanhKeo;
                    var dmDoHop = await context.DanhMucs.FirstOrDefaultAsync(d => d.TenDanhMuc != null && d.TenDanhMuc.Contains("Đồ hộp")) ?? dmBanhKeo;
                    var dmTayRua = await context.DanhMucs.FirstOrDefaultAsync(d => d.TenDanhMuc != null && d.TenDanhMuc.Contains("Tẩy rửa")) ?? dmBanhKeo;

                    var dvtThung = await context.DonViTinhs.FirstOrDefaultAsync(d => d.TenDonVi == "Thùng") ?? await context.DonViTinhs.FirstAsync();
                    var dvtHop = await context.DonViTinhs.FirstOrDefaultAsync(d => d.TenDonVi == "Hộp") ?? dvtThung;
                    var dvtChai = await context.DonViTinhs.FirstOrDefaultAsync(d => d.TenDonVi == "Chai") ?? dvtThung;
                    var dvtGoi = await context.DonViTinhs.FirstOrDefaultAsync(d => d.TenDonVi == "Gói") ?? dvtThung;
                    var dvtLon = await context.DonViTinhs.FirstOrDefaultAsync(d => d.TenDonVi == "Lon") ?? dvtThung;
                    var dvtLoc = await context.DonViTinhs.FirstOrDefaultAsync(d => d.TenDonVi == "Lốc") ?? dvtThung;
                    var dvtCan = await context.DonViTinhs.FirstOrDefaultAsync(d => d.TenDonVi == "Can") ?? dvtThung;

                    var sampleProducts = new List<(SanPham sp, int stock)>
                    {
                        (new SanPham { MaSanPhamCode = "SP001", TenSanPham = "Bánh quy bơ Hoàng Gia Danisa 454g", MaDanhMuc = dmBanhKeo.MaDanhMuc, MaDonViTinh = dvtHop.MaDonViTinh, GiaMacDinh = 128000, AnhSanPham = "https://images.unsplash.com/photo-1558961363-fa8fdf82db35?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 85),
                        (new SanPham { MaSanPhamCode = "SP002", TenSanPham = "Kẹo mềm dẻo hương trái cây Alpenliebe 330g", MaDanhMuc = dmBanhKeo.MaDanhMuc, MaDonViTinh = dvtGoi.MaDonViTinh, GiaMacDinh = 48000, AnhSanPham = "https://images.unsplash.com/photo-1582058091505-f87a2e55a40f?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 140),
                        (new SanPham { MaSanPhamCode = "SP003", TenSanPham = "Trà xanh đóng chai không độ 500ml (Thùng 24)", MaDanhMuc = dmDoUong.MaDanhMuc, MaDonViTinh = dvtThung.MaDonViTinh, GiaMacDinh = 185000, AnhSanPham = "https://images.unsplash.com/photo-1556881286-fc6915169721?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 60),
                        (new SanPham { MaSanPhamCode = "SP004", TenSanPham = "Dầu ăn thực vật Neptune Gold 1L", MaDanhMuc = dmGiaVi.MaDanhMuc, MaDonViTinh = dvtChai.MaDonViTinh, GiaMacDinh = 56000, AnhSanPham = "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 95),
                        (new SanPham { MaSanPhamCode = "SP005", TenSanPham = "Sữa tươi tiệt trùng Vinamilk 100% 1L", MaDanhMuc = dmSua.MaDanhMuc, MaDonViTinh = dvtHop.MaDonViTinh, GiaMacDinh = 39000, AnhSanPham = "https://images.unsplash.com/photo-1563636619-e9143da7973b?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 110),
                        (new SanPham { MaSanPhamCode = "SP006", TenSanPham = "Mì ăn liền Hảo Hảo tôm chua cay 75g (Thùng 30)", MaDanhMuc = dmGiaVi.MaDanhMuc, MaDonViTinh = dvtThung.MaDonViTinh, GiaMacDinh = 118000, AnhSanPham = "https://images.unsplash.com/photo-1612927601601-6638404737ce?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 15),
                        (new SanPham { MaSanPhamCode = "SP007", TenSanPham = "Nước mắm Nam Ngư Đệ Nhị 900ml", MaDanhMuc = dmGiaVi.MaDanhMuc, MaDonViTinh = dvtChai.MaDonViTinh, GiaMacDinh = 32000, AnhSanPham = "https://images.unsplash.com/photo-1589301760014-d929f3979dbc?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 75),
                        (new SanPham { MaSanPhamCode = "SP008", TenSanPham = "Tương ớt Chinsu chai 250g", MaDanhMuc = dmGiaVi.MaDanhMuc, MaDonViTinh = dvtChai.MaDonViTinh, GiaMacDinh = 16000, AnhSanPham = "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 120),
                        (new SanPham { MaSanPhamCode = "SP009", TenSanPham = "Cà phê hòa tan G7 3in1 Hộp 18 gói", MaDanhMuc = dmDoUong.MaDanhMuc, MaDonViTinh = dvtHop.MaDonViTinh, GiaMacDinh = 64000, AnhSanPham = "https://images.unsplash.com/photo-1514432324607-a09d9b4aefdd?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 45),
                        (new SanPham { MaSanPhamCode = "SP010", TenSanPham = "Nước ngọt Coca-Cola Sleek lon 320ml (Thùng 24)", MaDanhMuc = dmDoUong.MaDanhMuc, MaDonViTinh = dvtThung.MaDonViTinh, GiaMacDinh = 210000, AnhSanPham = "https://images.unsplash.com/photo-1622483767028-3f66f32aef97?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 18),
                        (new SanPham { MaSanPhamCode = "SP011", TenSanPham = "Bánh Chocopie Orion Hộp 12 cái", MaDanhMuc = dmBanhKeo.MaDanhMuc, MaDonViTinh = dvtHop.MaDonViTinh, GiaMacDinh = 58000, AnhSanPham = "https://images.unsplash.com/photo-1606313564200-e75d5e30476c?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 65),
                        (new SanPham { MaSanPhamCode = "SP012", TenSanPham = "Sữa chua ăn có đường Vinamilk Lốc 4 hộp", MaDanhMuc = dmSua.MaDanhMuc, MaDonViTinh = dvtLoc.MaDonViTinh, GiaMacDinh = 28000, AnhSanPham = "https://images.unsplash.com/photo-1488477181946-6428a0291777?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 90),
                        (new SanPham { MaSanPhamCode = "SP013", TenSanPham = "Nước khoáng thiên nhiên LaVie 500ml (Thùng 24)", MaDanhMuc = dmDoUong.MaDanhMuc, MaDonViTinh = dvtThung.MaDonViTinh, GiaMacDinh = 98000, AnhSanPham = "https://images.unsplash.com/photo-1548839140-29a749e1bc4e?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 80),
                        (new SanPham { MaSanPhamCode = "SP014", TenSanPham = "Cá nục sốt cà chua 3 Cô Gái Hộp 155g", MaDanhMuc = dmDoHop.MaDanhMuc, MaDonViTinh = dvtLon.MaDonViTinh, GiaMacDinh = 19500, AnhSanPham = "https://images.unsplash.com/photo-1534483509719-3feaee7c30da?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 130),
                        (new SanPham { MaSanPhamCode = "SP015", TenSanPham = "Nước giặt OMO Matic Cửa Trên Túi 3.6kg", MaDanhMuc = dmTayRua.MaDanhMuc, MaDonViTinh = dvtCan.MaDonViTinh, GiaMacDinh = 189000, AnhSanPham = "https://images.unsplash.com/photo-1610557892470-55d9e80c0bce?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 35),
                        (new SanPham { MaSanPhamCode = "SP016", TenSanPham = "Nước rửa chén Sunlight Chanh Can 3.8kg", MaDanhMuc = dmTayRua.MaDanhMuc, MaDonViTinh = dvtCan.MaDonViTinh, GiaMacDinh = 115000, AnhSanPham = "https://images.unsplash.com/photo-1585670210693-e7fdd16b142e?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 50),
                        (new SanPham { MaSanPhamCode = "SP017", TenSanPham = "Dầu đậu nành nguyên chất Simply 2L", MaDanhMuc = dmGiaVi.MaDanhMuc, MaDonViTinh = dvtChai.MaDonViTinh, GiaMacDinh = 118000, AnhSanPham = "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 40),
                        (new SanPham { MaSanPhamCode = "SP018", TenSanPham = "Hạt nêm Knorr thịt thăn xương ống Gói 900g", MaDanhMuc = dmGiaVi.MaDanhMuc, MaDonViTinh = dvtGoi.MaDonViTinh, GiaMacDinh = 76000, AnhSanPham = "https://images.unsplash.com/photo-1596040033229-a9821ebd058d?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 70),
                        (new SanPham { MaSanPhamCode = "SP019", TenSanPham = "Phô mai Con Bò Cười Hộp 8 miếng 112g", MaDanhMuc = dmSua.MaDanhMuc, MaDonViTinh = dvtHop.MaDonViTinh, GiaMacDinh = 37000, AnhSanPham = "https://images.unsplash.com/photo-1486297678162-eb2a19b0a32d?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 8),
                        (new SanPham { MaSanPhamCode = "SP020", TenSanPham = "Trà thảo mộc Dr Thanh 330ml (Thùng 24)", MaDanhMuc = dmDoUong.MaDanhMuc, MaDonViTinh = dvtThung.MaDonViTinh, GiaMacDinh = 225000, AnhSanPham = "https://images.unsplash.com/photo-1556881286-fc6915169721?w=300&auto=format&fit=crop&q=60", TrangThai = true }, 55)
                    };

                    foreach (var item in sampleProducts)
                    {
                        if (!await context.SanPhams.AnyAsync(s => s.MaSanPhamCode == item.sp.MaSanPhamCode))
                        {
                            context.SanPhams.Add(item.sp);
                            await context.SaveChangesAsync();

                            // Tạo tồn kho
                            if (!await context.TonKhos.AnyAsync(t => t.MaSanPham == item.sp.MaSanPham))
                            {
                                context.TonKhos.Add(new TonKho
                                {
                                    MaSanPham = item.sp.MaSanPham,
                                    SoLuongTon = item.stock,
                                    NgayCapNhat = DateTime.Now
                                });
                                await context.SaveChangesAsync();
                            }
                        }
                    }
                }

                // 6.2. Đảm bảo có sẵn các mặt hàng tồn kho thấp mẫu (< 20) để kích hoạt hệ thống cảnh báo
                try
                {
                    var lowStockCount = await context.TonKhos.CountAsync(t => (t.SoLuongTon ?? 0) < 20);
                    if (lowStockCount < 3)
                    {
                        var anyTk = await context.TonKhos.OrderBy(t => t.MaTonKho).Take(3).ToListAsync();
                        if (anyTk.Count > 0) anyTk[0].SoLuongTon = 8;   // Rất thấp
                        if (anyTk.Count > 1) anyTk[1].SoLuongTon = 15;  // Tồn thấp
                        if (anyTk.Count > 2) anyTk[2].SoLuongTon = 18;  // Cận mức an toàn
                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception ex) { Console.WriteLine($"[Ensure LowStock]: {ex.Message}"); }
            }
            catch (Exception ex) { Console.WriteLine($"[Seed SanPham]: {ex.Message}"); }

            // 7. Seed Phiếu nhập hàng & Chi tiết phiếu nhập mẫu
            try
            {
                var existingPnCount = await context.PhieuNhaps.CountAsync();
                if (existingPnCount < 6)
                {
                    var adminUser = await context.NguoiDungs.FirstOrDefaultAsync();
                    var allNccs = await context.NhaCungCaps.ToListAsync();
                    var allSps = await context.SanPhams.ToListAsync();

                    if (adminUser != null && allNccs.Count >= 4 && allSps.Count >= 10)
                    {
                        var poData = new List<(string code, int nccIndex, int daysAgo, string status, List<(int spIndex, int qty, decimal price)> items)>
                        {
                            ("PN0001", 0, 45, "xacnhan", new List<(int, int, decimal)> { (0, 50, 120000), (1, 100, 45000), (5, 80, 115000) }),
                            ("PN0002", 1, 38, "xacnhan", new List<(int, int, decimal)> { (3, 60, 53000), (6, 50, 30000), (7, 100, 15000) }),
                            ("PN0003", 2, 30, "xacnhan", new List<(int, int, decimal)> { (4, 120, 37000), (11, 80, 26000), (18, 40, 35000) }),
                            ("PN0004", 3, 22, "xacnhan", new List<(int, int, decimal)> { (2, 70, 180000), (9, 50, 205000), (19, 60, 220000) }),
                            ("PN0005", 4, 15, "xacnhan", new List<(int, int, decimal)> { (8, 90, 60000), (12, 100, 95000) }),
                            ("PN0006", 5, 10, "xacnhan", new List<(int, int, decimal)> { (3, 80, 54000), (16, 40, 115000) }),
                            ("PN0007", 7, 5, "xacnhan", new List<(int, int, decimal)> { (14, 50, 182000), (15, 60, 110000) }),
                            ("PN0008", 0, 1, "nhap", new List<(int, int, decimal)> { (0, 30, 125000), (5, 50, 116000) })
                        };

                        foreach (var po in poData)
                        {
                            if (!await context.PhieuNhaps.AnyAsync(p => p.MaPhieuNhapCode == po.code))
                            {
                                var ncc = allNccs[po.nccIndex % allNccs.Count];
                                var newPn = new PhieuNhap
                                {
                                    MaPhieuNhapCode = po.code,
                                    MaNhaCungCap = ncc.MaNhaCungCap,
                                    MaNguoiDung = adminUser.MaNguoiDung,
                                    NgayNhap = DateTime.Today.AddDays(-po.daysAgo),
                                    NgayTao = DateTime.Now.AddDays(-po.daysAgo),
                                    TrangThai = po.status,
                                    TongTien = 0
                                };

                                context.PhieuNhaps.Add(newPn);
                                await context.SaveChangesAsync();

                                decimal totalSum = 0;
                                foreach (var item in po.items)
                                {
                                    var sp = allSps[item.spIndex % allSps.Count];
                                    var ct = new ChiTietPhieuNhap
                                    {
                                        MaPhieuNhap = newPn.MaPhieuNhap,
                                        MaSanPham = sp.MaSanPham,
                                        SoLuong = item.qty,
                                        DonGia = item.price
                                    };
                                    context.ChiTietPhieuNhaps.Add(ct);
                                    totalSum += (item.qty * item.price);

                                    if (po.status == "xacnhan")
                                    {
                                        context.LichSuNhapHangs.Add(new LichSuNhapHang
                                        {
                                            MaPhieuNhap = newPn.MaPhieuNhap,
                                            MaSanPham = sp.MaSanPham,
                                            SoLuongThayDoi = item.qty,
                                            LoaiThayDoi = "nhap",
                                            NgayTao = newPn.NgayNhap
                                        });
                                    }
                                }

                                newPn.TongTien = totalSum;
                                await context.SaveChangesAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"[Seed PhieuNhap]: {ex.Message}"); }

            // 7.2. Tự động đồng bộ và nạp Lịch sử biến động kho phong phú
            try
            {
                var existingLsCount = await context.LichSuNhapHangs.CountAsync();
                if (existingLsCount < 10)
                {
                    var allChiTiets = await context.ChiTietPhieuNhaps
                        .Include(c => c.PhieuNhap)
                        .ToListAsync();

                    foreach (var ct in allChiTiets)
                    {
                        if (ct.MaSanPham.HasValue && ct.MaPhieuNhap.HasValue)
                        {
                            var hasLog = await context.LichSuNhapHangs.AnyAsync(l => l.MaPhieuNhap == ct.MaPhieuNhap && l.MaSanPham == ct.MaSanPham);
                            if (!hasLog)
                            {
                                context.LichSuNhapHangs.Add(new LichSuNhapHang
                                {
                                    MaPhieuNhap = ct.MaPhieuNhap,
                                    MaSanPham = ct.MaSanPham.Value,
                                    SoLuongThayDoi = ct.SoLuong ?? 10,
                                    LoaiThayDoi = "nhap",
                                    NgayTao = ct.PhieuNhap?.NgayNhap ?? DateTime.Now.AddDays(-10)
                                });
                            }
                        }
                    }

                    // Thêm một số bản ghi nhập đầu kỳ và điều chỉnh kho cho đa dạng
                    var samplePn = await context.PhieuNhaps.FirstOrDefaultAsync();
                    if (samplePn != null)
                    {
                        var sampleSps = await context.SanPhams.Take(8).ToListAsync();
                        int dayOffset = 50;
                        foreach (var sp in sampleSps)
                        {
                            if (!await context.LichSuNhapHangs.AnyAsync(l => l.MaSanPham == sp.MaSanPham))
                            {
                                context.LichSuNhapHangs.Add(new LichSuNhapHang
                                {
                                    MaPhieuNhap = samplePn.MaPhieuNhap,
                                    MaSanPham = sp.MaSanPham,
                                    SoLuongThayDoi = 60,
                                    LoaiThayDoi = "nhap",
                                    NgayTao = DateTime.Now.AddDays(-dayOffset)
                                });
                                dayOffset -= 4;
                            }
                        }
                    }

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex) { Console.WriteLine($"[Seed LichSuNhapHang]: {ex.Message}"); }

            // 8. Seed Kỳ đánh giá AHP chuẩn mẫu nếu chưa có
            try
            {
                var hasCompletedKy = await context.KyDanhGias.AnyAsync(k => k.KetQuaXepHangs.Any());
                if (!hasCompletedKy)
                {
                    // Xóa các kỳ rỗng dở dang nếu có
                    var incompleteKys = await context.KyDanhGias.Where(k => !k.KetQuaXepHangs.Any()).ToListAsync();
                    if (incompleteKys.Any())
                    {
                        context.KyDanhGias.RemoveRange(incompleteKys);
                        await context.SaveChangesAsync();
                    }

                    var admin = await context.NguoiDungs.FirstOrDefaultAsync(u => u.VaiTro == "admin") 
                                ?? await context.NguoiDungs.FirstOrDefaultAsync();

                    var tieuChis = await context.TieuChis.OrderBy(t => t.MaTieuChi).ToListAsync();
                    var nccs = await context.NhaCungCaps.OrderBy(n => n.MaNhaCungCap).ToListAsync();

                    if (admin != null && tieuChis.Count >= 4 && nccs.Count >= 4)
                    {
                        var ky = new KyDanhGia
                        {
                            TenKyDanhGia = $"Đánh giá NCC Quý 3/{DateTime.Now.Year}",
                            MaNguoiDung = admin.MaNguoiDung,
                            NgayTao = DateTime.Now.AddDays(-7),
                            TrangThai = "hoanthanh"
                        };
                        context.KyDanhGias.Add(ky);
                        await context.SaveChangesAsync();

                        // Ma trận so sánh cặp AHP 4x4 chuẩn: (Giá, Chất lượng, Thời gian, Uy tín)
                        double[,] matrix = new double[4, 4]
                        {
                            { 1.0, 1.0/3.0, 2.0, 0.5 },
                            { 3.0, 1.0, 5.0, 3.0 },
                            { 0.5, 0.2, 1.0, 1.0/3.0 },
                            { 2.0, 1.0/3.0, 3.0, 1.0 }
                        };

                        for (int i = 0; i < 4; i++)
                        {
                            for (int j = 0; j < 4; j++)
                            {
                                context.MaTranSoSanhs.Add(new MaTranSoSanh
                                {
                                    MaKyDanhGia = ky.MaKyDanhGia,
                                    MaTieuChiDong = tieuChis[i].MaTieuChi,
                                    MaTieuChiCot = tieuChis[j].MaTieuChi,
                                    GiaTri = (decimal)Math.Round(matrix[i, j], 3)
                                });
                            }
                        }

                        // Tính toán trọng số AHP
                        var ahpRes = ahpService.CalculateAHP(matrix);
                        for (int i = 0; i < 4; i++)
                        {
                            context.TrongSoTieuChis.Add(new TrongSoTieuChi
                            {
                                MaKyDanhGia = ky.MaKyDanhGia,
                                MaTieuChi = tieuChis[i].MaTieuChi,
                                TrongSo = (decimal)Math.Round(ahpRes.TrongSo[i], 3),
                                LambdaMax = (decimal)Math.Round(ahpRes.LambdaMax, 4),
                                CI = (decimal)Math.Round(ahpRes.CI, 4),
                                CR = (decimal)Math.Round(ahpRes.CR, 4)
                            });
                        }

                        // Điểm đánh giá thực tế từng tiêu chí cho các NCC (Thang 1-9 theo Saaty & DB constraint)
                        var scoresMatrix = new decimal[8, 4]
                        {
                            { 8.0m, 9.0m, 8.5m, 9.0m }, // Asia Foods: Chất lượng cao, uy tín cao
                            { 8.5m, 8.5m, 8.0m, 8.5m }, // Masan: Rất mạnh, quy mô lớn
                            { 7.5m, 9.0m, 8.0m, 9.0m }, // Vinamilk: Chất lượng tuyệt hảo
                            { 9.0m, 7.5m, 8.5m, 8.0m }, // Tân Hiệp Phát: Giá tốt, cung ứng nhanh
                            { 7.0m, 9.0m, 7.5m, 9.0m }, // Nestlé: Tiêu chuẩn quốc tế
                            { 8.5m, 8.0m, 8.5m, 8.0m }, // CALOFIC: Giá cạnh tranh
                            { 8.0m, 8.0m, 8.0m, 8.0m }, // Bibica: Đều đặn
                            { 7.5m, 9.0m, 8.0m, 8.5m }  // Unilever: Uy tín hàng đầu
                        };

                        int evaluateCount = Math.Min(nccs.Count, 8);
                        for (int s = 0; s < evaluateCount; s++)
                        {
                            for (int t = 0; t < 4; t++)
                            {
                                context.DiemDanhGiaNCCs.Add(new DiemDanhGiaNCC
                                {
                                    MaKyDanhGia = ky.MaKyDanhGia,
                                    MaNhaCungCap = nccs[s].MaNhaCungCap,
                                    MaTieuChi = tieuChis[t].MaTieuChi,
                                    Diem = Math.Clamp(scoresMatrix[s, t], 1.0m, 9.0m)
                                });
                            }
                        }

                        // Xếp hạng tổng hợp Weighted Sum
                        var weights = ahpRes.TrongSo;
                        var supplierScores = new List<(int MaNCC, double DiemTong)>();
                        for (int s = 0; s < evaluateCount; s++)
                        {
                            double[] cScores = new double[4];
                            for (int t = 0; t < 4; t++) cScores[t] = (double)scoresMatrix[s, t];
                            double tot = ahpService.TinhDiemWeightedSum(weights, cScores);
                            supplierScores.Add((nccs[s].MaNhaCungCap, tot));
                        }

                        var sorted = supplierScores.OrderByDescending(x => x.DiemTong).ToList();
                        for (int r = 1; r <= sorted.Count; r++)
                        {
                            context.KetQuaXepHangs.Add(new KetQuaXepHang
                            {
                                MaKyDanhGia = ky.MaKyDanhGia,
                                MaNhaCungCap = sorted[r - 1].MaNCC,
                                DiemTong = (decimal)Math.Round(sorted[r - 1].DiemTong, 2),
                                XepHang = r,
                                DeXuat = (r == 1)
                            });
                        }

                        await context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Seed KyDanhGia Warning]: {ex.Message} --> Inner: {ex.InnerException?.Message}");
            }
        }
    }
}

