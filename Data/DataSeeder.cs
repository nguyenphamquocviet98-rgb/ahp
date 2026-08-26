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

            // 1. Seed Người dùng mẫu nếu chưa có
            if (!await context.NguoiDungs.AnyAsync())
            {
                var users = new List<NguoiDung>
                {
                    new NguoiDung { TenDangNhap = "admin", MatKhau = "123456", VaiTro = "admin", TrangThai = true, NgayTao = DateTime.Now },
                    new NguoiDung { TenDangNhap = "quanly", MatKhau = "123456", VaiTro = "quanly", TrangThai = true, NgayTao = DateTime.Now },
                    new NguoiDung { TenDangNhap = "nhanvien", MatKhau = "123456", VaiTro = "nhanvien", TrangThai = true, NgayTao = DateTime.Now }
                };
                context.NguoiDungs.AddRange(users);
                await context.SaveChangesAsync();
            }

            // 2. Seed Tiêu chí AHP nếu chưa có
            if (!await context.TieuChis.AnyAsync())
            {
                var tieuChis = new List<TieuChi>
                {
                    new TieuChi { TenTieuChi = "Giá cả", LoaiTieuChi = "cost" },
                    new TieuChi { TenTieuChi = "Chất lượng hàng hóa", LoaiTieuChi = "benefit" },
                    new TieuChi { TenTieuChi = "Thời gian giao hàng", LoaiTieuChi = "cost" },
                    new TieuChi { TenTieuChi = "Độ uy tín & Dịch vụ", LoaiTieuChi = "benefit" }
                };
                context.TieuChis.AddRange(tieuChis);
                await context.SaveChangesAsync();
            }

            // 3. Seed Danh mục & Đơn vị tính nếu chưa có
            if (!await context.DanhMucs.AnyAsync())
            {
                var danhMucs = new List<DanhMuc>
                {
                    new DanhMuc { TenDanhMuc = "Bánh kẹo & Đồ ngọt", MoTa = "Các loại bánh quy, kẹo dẻo, socola" },
                    new DanhMuc { TenDanhMuc = "Đồ uống & Giải khát", MoTa = "Nước ngọt, trà xanh, nước khoáng" },
                    new DanhMuc { TenDanhMuc = "Gia vị & Thực phẩm khô", MoTa = "Dầu ăn, nước mắm, đường, mì gói" },
                    new DanhMuc { TenDanhMuc = "Sữa & Chế phẩm từ sữa", MoTa = "Sữa tươi, sữa chua, phô mai" }
                };
                context.DanhMucs.AddRange(danhMucs);
                await context.SaveChangesAsync();
            }

            if (!await context.DonViTinhs.AnyAsync())
            {
                var dvts = new List<DonViTinh>
                {
                    new DonViTinh { TenDonVi = "Thùng" },
                    new DonViTinh { TenDonVi = "Hộp" },
                    new DonViTinh { TenDonVi = "Chai" },
                    new DonViTinh { TenDonVi = "Gói" },
                    new DonViTinh { TenDonVi = "Kg" }
                };
                context.DonViTinhs.AddRange(dvts);
                await context.SaveChangesAsync();
            }

            // 4. Seed Nhà cung cấp nếu chưa có
            if (!await context.NhaCungCaps.AnyAsync())
            {
                var nccs = new List<NhaCungCap>
                {
                    new NhaCungCap { MaNhaCungCapCode = "NCC001", TenNhaCungCap = "Công ty TNHH Thực phẩm Á Châu", NguoiLienHe = "Nguyễn Văn Tuấn", SoDienThoai = "0912345678", Email = "achau@food.vn", DiaChi = "120 Nguyễn Huệ, Quận 1, TP.HCM", TrangThai = true },
                    new NhaCungCap { MaNhaCungCapCode = "NCC002", TenNhaCungCap = "Công ty CP Nông sản & Thực phẩm Việt", NguoiLienHe = "Phạm Thị Hương", SoDienThoai = "0987654321", Email = "huong@vietfood.com", DiaChi = "45 Lê Duẩn, Ba Đình, Hà Nội", TrangThai = true },
                    new NhaCungCap { MaNhaCungCapCode = "NCC003", TenNhaCungCap = "Công ty TNHH Thương mại Tân Phú", NguoiLienHe = "Trần Minh Đăng", SoDienThoai = "0908112233", Email = "tanphu@trade.vn", DiaChi = "78 Cách Mạng Tháng 8, Quận 3, TP.HCM", TrangThai = true },
                    new NhaCungCap { MaNhaCungCapCode = "NCC004", TenNhaCungCap = "Doanh nghiệp Tư nhân Hoàng Gia", NguoiLienHe = "Lê Hoàng Quân", SoDienThoai = "0933445566", Email = "hoanggia@supply.vn", DiaChi = "234 Võ Văn Tần, Quận 3, TP.HCM", TrangThai = true }
                };
                context.NhaCungCaps.AddRange(nccs);
                await context.SaveChangesAsync();
            }

            // 5. Seed Sản phẩm & Tồn kho nếu chưa có
            if (!await context.SanPhams.AnyAsync())
            {
                var dm = await context.DanhMucs.FirstAsync();
                var dvt = await context.DonViTinhs.FirstAsync();

                var spList = new List<SanPham>
                {
                    new SanPham { MaSanPhamCode = "SP001", TenSanPham = "Bánh quy bơ Danisa 454g", MaDanhMuc = dm.MaDanhMuc, MaDonViTinh = dvt.MaDonViTinh, GiaMacDinh = 125000, TrangThai = true },
                    new SanPham { MaSanPhamCode = "SP002", TenSanPham = "Kẹo mềm dẻo hương trái cây Alpenliebe", MaDanhMuc = dm.MaDanhMuc, MaDonViTinh = dvt.MaDonViTinh, GiaMacDinh = 45000, TrangThai = true },
                    new SanPham { MaSanPhamCode = "SP003", TenSanPham = "Trà xanh đóng chai không độ 500ml", MaDanhMuc = dm.MaDanhMuc, MaDonViTinh = dvt.MaDonViTinh, GiaMacDinh = 180000, TrangThai = true },
                    new SanPham { MaSanPhamCode = "SP004", TenSanPham = "Dầu ăn thực vật cao cấp Neptune 1L", MaDanhMuc = dm.MaDanhMuc, MaDonViTinh = dvt.MaDonViTinh, GiaMacDinh = 52000, TrangThai = true },
                    new SanPham { MaSanPhamCode = "SP005", TenSanPham = "Sữa tươi tiệt trùng Vinamilk 100% 1L", MaDanhMuc = dm.MaDanhMuc, MaDonViTinh = dvt.MaDonViTinh, GiaMacDinh = 38000, TrangThai = true }
                };

                context.SanPhams.AddRange(spList);
                await context.SaveChangesAsync();

                // Tạo tồn kho ban đầu cho từng sản phẩm
                foreach (var sp in spList)
                {
                    context.TonKhos.Add(new TonKho
                    {
                        MaSanPham = sp.MaSanPham,
                        SoLuongTon = 50,
                        NgayCapNhat = DateTime.Now
                    });
                }
                await context.SaveChangesAsync();
            }

            // 6. Seed Kỳ đánh giá AHP chuẩn mẫu nếu chưa có
            if (!await context.KyDanhGias.AnyAsync())
            {
                var admin = await context.NguoiDungs.FirstAsync(u => u.VaiTro == "admin");
                var ky = new KyDanhGia
                {
                    TenKyDanhGia = $"Đánh giá Nhà cung cấp Quý {((DateTime.Now.Month - 1) / 3) + 1}/{DateTime.Now.Year}",
                    MaNguoiDung = admin.MaNguoiDung,
                    NgayTao = DateTime.Now,
                    TrangThai = "hoanthanh"
                };
                context.KyDanhGias.Add(ky);
                await context.SaveChangesAsync();

                var tieuChis = await context.TieuChis.OrderBy(t => t.MaTieuChi).ToListAsync();
                var nccs = await context.NhaCungCaps.OrderBy(n => n.MaNhaCungCap).ToListAsync();

                // Ma trận 4x4 chuẩn: (Giá: 1/3 Chất lượng, 2 Thời gian, 1/2 Uy tín, CL: 5 Thời gian, 3 Uy tín, TG: 1/3 Uy tín)
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

                // Tính toán AHP
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

                // Chấm điểm mẫu từng NCC
                var scoresMatrix = new decimal[4, 4]
                {
                    { 7.0m, 8.5m, 7.5m, 8.0m }, // NCC 1: Chất lượng cao, uy tín cao
                    { 9.0m, 5.5m, 4.5m, 5.0m }, // NCC 2: Giá rẻ
                    { 5.0m, 7.0m, 8.5m, 6.5m }, // NCC 3: Giao hàng nhanh
                    { 6.5m, 6.0m, 6.0m, 6.0m }  // NCC 4: Trung bình
                };

                for (int s = 0; s < Math.Min(nccs.Count, 4); s++)
                {
                    for (int t = 0; t < 4; t++)
                    {
                        context.DiemDanhGiaNCCs.Add(new DiemDanhGiaNCC
                        {
                            MaKyDanhGia = ky.MaKyDanhGia,
                            MaNhaCungCap = nccs[s].MaNhaCungCap,
                            MaTieuChi = tieuChis[t].MaTieuChi,
                            Diem = scoresMatrix[s, t]
                        });
                    }
                }

                // Xếp hạng Weighted Sum
                var weights = ahpRes.TrongSo;
                var supplierScores = new List<(int MaNCC, double DiemTong)>();
                for (int s = 0; s < Math.Min(nccs.Count, 4); s++)
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
}
