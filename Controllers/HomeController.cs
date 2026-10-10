using code.Services;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;
using code.Models.ViewModels;

namespace code.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Trang chủ hệ thống:
    /// - Nếu là Admin Sàn: Chuyển hướng thẳng tới Dashboard Quản trị Sàn B2B
    /// - Nếu là NPP (Chủ/NV): Chuyển vào Dashboard quản lý kho nội bộ
    /// - Nếu chưa đăng nhập: Hiển thị Landing Page Cổng Sàn B2B & AHP
    /// </summary>
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            if (role == "admin_san" || role == "admin")
            {
                return RedirectToAction("Index", "DashboardAdmin");
            }
            return await GetDashboardView();
        }

        return await GetLandingView();
    }

    /// <summary>
    /// Bảng điều khiển quản lý nội bộ (Dành cho người dùng đã đăng nhập)
    /// </summary>
    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        if (role == "admin_san" || role == "admin")
        {
            return RedirectToAction("Index", "DashboardAdmin");
        }
        return await GetDashboardView();
    }

    /// <summary>
    /// Xem Cổng Sàn B2B công khai (Người đã đăng nhập vẫn có thể xem lại nếu muốn)
    /// </summary>
    [AllowAnonymous]
    public async Task<IActionResult> Landing()
    {
        return await GetLandingView();
    }

    // ===== Helper: Nạp dữ liệu Dashboard nội bộ =====
    private async Task<IActionResult> GetDashboardView()
    {
        if (User.CompanyId() > 0)
        {
            var stocks = await _context.TonKhoNPPs.Include(t => t.SanPham).ThenInclude(s => s!.DonViTinh)
                .Where(t => t.MaDoanhNghiep == User.CompanyId()).ToListAsync();
            var converted = stocks.Select(t => new TonKho { MaSanPham = t.MaSanPham, SoLuongTon = t.SoLuongTon, SanPham = t.SanPham }).ToList();
            var purchases = await _context.DonHangB2Bs.Where(d => d.MaNPPMua == User.CompanyId()).ToListAsync();
            return View("Dashboard", new DashboardViewModel
            {
                TongSanPham = stocks.Count, TongSoLuongTon = stocks.Sum(t => t.SoLuongTon),
                TongNhaCungCap = purchases.Select(p => p.MaNPPBan).Distinct().Count(),
                TongPhieuNhap = await _context.PhieuNhaps.CountAsync(),
                TongGiaTriNhap = purchases.Where(p => p.TrangThai == "hoan_thanh").Sum(p => p.TongTien)
                    + await _context.PhieuNhaps.Where(p => p.TrangThai == "xacnhan" && p.PhienBanNhapKho >= 2).SumAsync(p => p.TongTien ?? 0),
                DanhSachTonKho = converted.OrderBy(t => t.SoLuongTon).Take(5).ToList(),
                DanhSachCanhBaoTonKho = converted.Where(t => t.SoLuongTon < 20).ToList(),
                SoMatHangCanhBao = converted.Count(t => t.SoLuongTon < 20),
                PhieuNhapMoiNhat = await _context.PhieuNhaps.Include(p => p.NhaCungCap).Include(p => p.NguoiDung)
                    .Include(p => p.ChiTietPhieuNhaps).OrderByDescending(p => p.NgayTao).Take(5).ToListAsync()
            });
        }
        // 1. Thống kê danh mục sản phẩm từ SQL
        var thongKeDanhMuc = await _context.DanhMucs
            .Select(d => new DanhMucThongKeDto
            {
                TenDanhMuc = d.TenDanhMuc ?? "Chưa phân loại",
                SoLuongSanPham = d.SanPhams.Count
            })
            .Where(d => d.SoLuongSanPham > 0)
            .ToListAsync();

        // 2. Thống kê điểm số AHP các nhà cung cấp từ kỳ đánh giá gần nhất trong SQL
        var kyAHPGanNhat = await _context.KyDanhGias
            .Include(k => k.KetQuaXepHangs)
                .ThenInclude(kq => kq.NhaCungCap)
            .Where(k => k.KetQuaXepHangs.Any())
            .OrderByDescending(k => k.MaKyDanhGia)
            .FirstOrDefaultAsync();

        var thongKeNhaCungCapAHP = kyAHPGanNhat?.KetQuaXepHangs
            .OrderBy(kq => kq.XepHang)
            .Select(kq => new NhaCungCapDiemDto
            {
                TenNhaCungCap = kq.NhaCungCap != null ? kq.NhaCungCap.TenNhaCungCap ?? "N/A" : "N/A",
                DiemTong = kq.DiemTong ?? 0,
                XepHang = kq.XepHang ?? 99,
                DeXuat = kq.DeXuat ?? false
            })
            .ToList() ?? new List<NhaCungCapDiemDto>();

        // 3. Thống kê xu hướng chi phí nhập hàng theo 6 tháng gần nhất từ SQL
        var sixMonthsAgo = DateTime.Today.AddMonths(-5);
        var startOfPeriod = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

        var phieuNhapsTrongKy = await _context.PhieuNhaps
            .Where(p => (p.NgayNhap ?? p.NgayTao) >= startOfPeriod && p.TrangThai != "huy")
            .ToListAsync();

        var thongKeChiPhiTheoThang = new List<ChiPhiTheoThangDto>();
        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = DateTime.Today.AddMonths(-i);
            var m = targetMonth.Month;
            var y = targetMonth.Year;

            var inMonth = phieuNhapsTrongKy
                .Where(p => {
                    var dt = p.NgayNhap ?? p.NgayTao ?? DateTime.MinValue;
                    return dt.Month == m && dt.Year == y;
                })
                .ToList();

            thongKeChiPhiTheoThang.Add(new ChiPhiTheoThangDto
            {
                ThangNam = $"T{m:D2}/{y}",
                TongTien = inMonth.Sum(p => p.TongTien ?? 0),
                SoPhieuNhap = inMonth.Count
            });
        }

        // 4. Danh sách cảnh báo tồn kho thấp (< 20)
        var canhBaoTonKho = await _context.TonKhos
            .Include(t => t.SanPham)
                .ThenInclude(s => s!.DonViTinh)
            .Include(t => t.SanPham)
                .ThenInclude(s => s!.DanhMuc)
            .Where(t => (t.SoLuongTon ?? 0) < 20)
            .OrderBy(t => t.SoLuongTon)
            .ToListAsync();

        var viewModel = new DashboardViewModel
        {
            TongSanPham = await _context.SanPhams.CountAsync(),
            TongNhaCungCap = await _context.NhaCungCaps.CountAsync(),
            TongPhieuNhap = await _context.PhieuNhaps.CountAsync(),
            TongGiaTriNhap = await _context.ChiTietPhieuNhaps.AnyAsync()
                ? await _context.ChiTietPhieuNhaps.SumAsync(c => c.ThanhTien ?? (c.SoLuong * c.DonGia) ?? 0)
                : await _context.PhieuNhaps.SumAsync(p => p.TongTien ?? 0),
            TongSoLuongTon = await _context.TonKhos.SumAsync(t => t.SoLuongTon ?? 0),

            PhieuNhapMoiNhat = await _context.PhieuNhaps
                .Include(p => p.NhaCungCap)
                .Include(p => p.NguoiDung)
                .Include(p => p.ChiTietPhieuNhaps)
                .OrderByDescending(p => p.NgayNhap ?? p.NgayTao)
                .Take(5)
                .ToListAsync(),

            DanhSachTonKho = await _context.TonKhos
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DonViTinh)
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DanhMuc)
                .OrderBy(t => t.SoLuongTon)
                .Take(5)
                .ToListAsync(),

            NhaCungCapDeXuatGanNhat = await _context.KetQuaXepHangs
                .Include(k => k.NhaCungCap)
                .Include(k => k.KyDanhGia)
                .Where(k => k.DeXuat == true)
                .OrderByDescending(k => k.MaKyDanhGia)
                .FirstOrDefaultAsync(),

            SoMatHangCanhBao = canhBaoTonKho.Count,
            DanhSachCanhBaoTonKho = canhBaoTonKho,

            ThongKeDanhMuc = thongKeDanhMuc,
            ThongKeNhaCungCapAHP = thongKeNhaCungCapAHP,
            ThongKeChiPhiTheoThang = thongKeChiPhiTheoThang
        };

        return View("Dashboard", viewModel);
    }

    // ===== Helper: Nạp dữ liệu Landing Page Cổng B2B công khai =====
    private async Task<IActionResult> GetLandingView()
    {
        // 1. Danh mục ngành hàng
        var danhMucs = await _context.DanhMucs
            .Include(d => d.SanPhams)
            .Select(d => new DanhMucLandingDto
            {
                MaDanhMuc = d.MaDanhMuc,
                TenDanhMuc = d.TenDanhMuc ?? "Tổng hợp",
                SoLuongSanPham = d.SanPhams.Count,
                IconClass = (d.TenDanhMuc ?? "").Contains("uống") || (d.TenDanhMuc ?? "").Contains("Giải khát") ? "bi-cup-straw" :
                            (d.TenDanhMuc ?? "").Contains("Thực phẩm") || (d.TenDanhMuc ?? "").Contains("Ăn") ? "bi-egg-fried" :
                            (d.TenDanhMuc ?? "").Contains("Hóa") || (d.TenDanhMuc ?? "").Contains("Mỹ phẩm") ? "bi-shield-check" :
                            (d.TenDanhMuc ?? "").Contains("Gia vị") ? "bi-flower1" :
                            (d.TenDanhMuc ?? "").Contains("Bánh") || (d.TenDanhMuc ?? "").Contains("Kẹo") ? "bi-cookie" : "bi-box-seam"
            })
            .ToListAsync();

        // 2. Danh sách Nhà Phân Phối / Doanh Nghiệp uy tín
        var nhaPhanPhois = await _context.DoanhNghieps
            .Where(dn => dn.TrangThai)
            .OrderByDescending(dn => dn.LaGianHangMall)
            .ThenByDescending(dn => dn.DiemUyTin)
            .Take(8)
            .Select(dn => new NhaPhanPhoiLandingDto
            {
                MaDoanhNghiep = dn.MaDoanhNghiep,
                TenDoanhNghiep = dn.TenDoanhNghiep,
                DiaChi = dn.DiaChi ?? "Toàn quốc",
                SoDienThoai = dn.SoDienThoai ?? "0908 123 456",
                DiemUyTin = dn.DiemUyTin,
                LaGianHangMall = dn.LaGianHangMall,
                SoSanPhamChaoBan = dn.TonKhoNPPs.Count(t => t.DangChaoBan),
                Logo = dn.Logo,
                MoTa = dn.MoTa ?? "Nhà phân phối sỉ uy tín, cung ứng đa dạng mặt hàng tiêu dùng nhanh với chính sách chiết khấu cao.",
                CoDeXuatAHP = dn.LaGianHangMall || dn.DiemUyTin >= 8.5m
            })
            .ToListAsync();

        // Nếu bảng DoanhNghiep chưa có đủ dữ liệu, bổ sung từ Nhà Cung Cấp truyền thống
        if (nhaPhanPhois.Count < 3)
        {
            var nccs = await _context.NhaCungCaps
                .Take(6)
                .ToListAsync();

            foreach (var ncc in nccs)
            {
                if (!nhaPhanPhois.Any(np => np.TenDoanhNghiep.Contains(ncc.TenNhaCungCap ?? "")))
                {
                    nhaPhanPhois.Add(new NhaPhanPhoiLandingDto
                    {
                        MaDoanhNghiep = ncc.MaNhaCungCap,
                        TenDoanhNghiep = ncc.TenNhaCungCap ?? "Nhà cung cấp uy tín",
                        DiaChi = ncc.DiaChi ?? "TP. Hồ Chí Minh",
                        SoDienThoai = ncc.SoDienThoai ?? "0912 345 678",
                        DiemUyTin = 9.0m,
                        LaGianHangMall = true,
                        SoSanPhamChaoBan = 12,
                        MoTa = "Đơn vị cung ứng đối tác chiến lược trong chuỗi phân phối hàng hóa tiêu dùng.",
                        CoDeXuatAHP = true
                    });
                }
            }
        }

        // 3. Danh sách sản phẩm B2B giá sỉ tiêu biểu
        var sanPhamB2B = await _context.TonKhoNPPs
            .Include(t => t.SanPham)
                .ThenInclude(s => s!.DanhMuc)
            .Include(t => t.SanPham)
                .ThenInclude(s => s!.DonViTinh)
            .Include(t => t.DoanhNghiep)
            .Where(t => t.DangChaoBan && t.DoanhNghiep != null && t.DoanhNghiep.TrangThai)
            .OrderByDescending(t => t.DoanhNghiep!.LaGianHangMall)
            .ThenByDescending(t => t.SoLuongTon)
            .Take(12)
            .Select(t => new SanPhamB2BLandingDto
            {
                MaSanPham = t.MaSanPham,
                MaTonKhoNPP = t.MaTonKhoNPP,
                TenSanPham = t.SanPham != null ? (t.SanPham.TenSanPham ?? "Sản phẩm") : "Sản phẩm",
                TenDanhMuc = (t.SanPham != null && t.SanPham.DanhMuc != null) ? t.SanPham.DanhMuc.TenDanhMuc : "Tổng hợp",
                DonViTinh = (t.SanPham != null && t.SanPham.DonViTinh != null) ? t.SanPham.DonViTinh.TenDonVi : "Thùng",
                GiaSi = t.GiaSi > 0 ? t.GiaSi : (t.SanPham != null && t.SanPham.GiaMacDinh.HasValue ? t.SanPham.GiaMacDinh.Value * 0.9m : 50000),
                GiaGoc = t.SanPham != null && t.SanPham.GiaMacDinh.HasValue && t.SanPham.GiaMacDinh.Value > 0 ? t.SanPham.GiaMacDinh.Value : (t.GiaSi * 1.15m),
                TenNhaCungCap = t.DoanhNghiep != null ? t.DoanhNghiep.TenDoanhNghiep : "Nhà phân phối",
                MaDoanhNghiep = t.MaDoanhNghiep,
                LaMall = t.DoanhNghiep != null && t.DoanhNghiep.LaGianHangMall,
                ThoiGianGiaoHang = t.ThoiGianGiaoHang,
                SoLuongCoSan = t.SoLuongTon,
                AnhSanPham = t.SanPham != null ? t.SanPham.AnhSanPham : null
            })
            .ToListAsync();

        // Nếu danh sách sản phẩm TonKhoNPP ít hơn 6, bổ sung từ bảng SanPham gốc
        if (sanPhamB2B.Count < 6)
        {
            var fallbackSanPhams = await _context.SanPhams
                .Include(s => s.DanhMuc)
                .Include(s => s.DonViTinh)
                .Take(10)
                .ToListAsync();

            foreach (var sp in fallbackSanPhams)
            {
                if (!sanPhamB2B.Any(p => p.MaSanPham == sp.MaSanPham))
                {
                    var giaGoc = sp.GiaMacDinh ?? 100000;
                    sanPhamB2B.Add(new SanPhamB2BLandingDto
                    {
                        MaSanPham = sp.MaSanPham,
                        TenSanPham = sp.TenSanPham ?? "Sản phẩm B2B",
                        TenDanhMuc = sp.DanhMuc?.TenDanhMuc ?? "Tổng hợp",
                        DonViTinh = sp.DonViTinh?.TenDonVi ?? "Thùng",
                        GiaSi = giaGoc * 0.88m,
                        GiaGoc = giaGoc,
                        TenNhaCungCap = nhaPhanPhois.FirstOrDefault()?.TenDoanhNghiep ?? "Tổng kho Phân phối Miền Nam",
                        MaDoanhNghiep = nhaPhanPhois.FirstOrDefault()?.MaDoanhNghiep ?? 1,
                        LaMall = true,
                        ThoiGianGiaoHang = 1,
                        SoLuongCoSan = 500,
                        AnhSanPham = sp.AnhSanPham
                    });
                }
            }
        }

        // 4. Danh sách tài khoản demo cho 1-click test
        var demoAccounts = await _context.NguoiDungs
            .Include(u => u.DoanhNghiep)
            .Where(u => u.TrangThai == true)
            .Take(6)
            .Select(u => new DemoAccountDto
            {
                MaNguoiDung = u.MaNguoiDung,
                TenDangNhap = u.TenDangNhap ?? "",
                HoTen = u.DoanhNghiep != null ? u.DoanhNghiep.TenDoanhNghiep : (u.TenDangNhap ?? "Người dùng"),
                VaiTro = u.VaiTro ?? "nhanvien_npp",
                TenDoanhNghiep = u.DoanhNghiep != null ? u.DoanhNghiep.TenDoanhNghiep : "Ban Quản Trị Sàn",
                LaMall = u.DoanhNghiep != null && u.DoanhNghiep.LaGianHangMall
            })
            .ToListAsync();

        // 5. Thống kê sàn tổng quan
        int totalDoanhNghiep = await _context.DoanhNghieps.CountAsync();
        int totalSanPham = await _context.SanPhams.CountAsync();
        int totalPhieuNhap = await _context.PhieuNhaps.CountAsync();
        int totalDonHangB2B = await _context.DonHangB2Bs.CountAsync();

        var landingModel = new LandingPageViewModel
        {
            DanhSachDanhMuc = danhMucs,
            DanhSachNhaPhanPhoi = nhaPhanPhois,
            DanhSachSanPhamHot = sanPhamB2B,
            TaiKhoanDemo = demoAccounts,
            TongNhaPhanPhoi = totalDoanhNghiep > 0 ? totalDoanhNghiep : (await _context.NhaCungCaps.CountAsync()),
            TongSanPham = totalSanPham > 0 ? totalSanPham : 250,
            TongGiaoDich = totalPhieuNhap + totalDonHangB2B,
            TiLeToiUuAHP = 99.4m
        };

        return View("Index", landingModel);
    }

    // API lấy nhanh danh sách cảnh báo tồn kho cho chuông thông báo Header
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetLowStockAlerts()
    {
        if (User.CompanyId() > 0)
        {
            var items = await _context.TonKhoNPPs.Where(t => t.MaDoanhNghiep == User.CompanyId() && t.SoLuongTon < 20)
                .Select(t => new { maSanPham = t.MaSanPham, maCode = t.SanPham!.MaSanPhamCode, tenSanPham = t.SanPham.TenSanPham,
                    soLuongTon = t.SoLuongTon, donViTinh = t.SanPham.DonViTinh!.TenDonVi, isOutOfStock = t.SoLuongTon == 0 }).ToListAsync();
            return Json(new { count = items.Count, items });
        }
        var lowStocks = await _context.TonKhos
            .Include(t => t.SanPham)
                .ThenInclude(s => s!.DonViTinh)
            .Where(t => (t.SoLuongTon ?? 0) < 20)
            .OrderBy(t => t.SoLuongTon)
            .Select(t => new
            {
                maSanPham = t.MaSanPham,
                maCode = t.SanPham != null ? t.SanPham.MaSanPhamCode : "",
                tenSanPham = t.SanPham != null ? t.SanPham.TenSanPham : "Sản phẩm",
                soLuongTon = t.SoLuongTon ?? 0,
                donViTinh = (t.SanPham != null && t.SanPham.DonViTinh != null) ? t.SanPham.DonViTinh.TenDonVi : "Cái",
                isOutOfStock = (t.SoLuongTon ?? 0) <= 0
            })
            .ToListAsync();

        return Json(new { count = lowStocks.Count, items = lowStocks });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
