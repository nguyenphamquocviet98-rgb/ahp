using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;
using code.Models.ViewModels;

namespace code.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
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
                TenNhaCungCap = kq.NhaCungCap != null ? kq.NhaCungCap.TenNhaCungCap : "N/A",
                DiemTong = kq.DiemTong ?? 0,
                XepHang = kq.XepHang ?? 99,
                DeXuat = kq.DeXuat ?? false
            })
            .ToList() ?? new List<NhaCungCapDiemDto>();

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

            ThongKeDanhMuc = thongKeDanhMuc,
            ThongKeNhaCungCapAHP = thongKeNhaCungCapAHP
        };

        return View(viewModel);
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
