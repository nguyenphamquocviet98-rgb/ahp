using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using code.Data;

namespace code.Controllers
{
    [Authorize]
    public class LichSuNhapHangController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LichSuNhapHangController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LichSuNhapHang
        public async Task<IActionResult> Index(string? search, string? loaiThayDoi)
        {
            var query = _context.LichSuNhapHangs
                .Include(l => l.SanPham)
                    .ThenInclude(s => s!.DonViTinh)
                .Include(l => l.PhieuNhap)
                    .ThenInclude(p => p!.NhaCungCap)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(l => (l.SanPham != null && l.SanPham.TenSanPham != null && l.SanPham.TenSanPham.Contains(s)) ||
                                         (l.SanPham != null && l.SanPham.MaSanPhamCode != null && l.SanPham.MaSanPhamCode.Contains(s)) ||
                                         (l.PhieuNhap != null && l.PhieuNhap.MaPhieuNhapCode != null && l.PhieuNhap.MaPhieuNhapCode.Contains(s)));
                ViewData["Search"] = s;
            }

            if (!string.IsNullOrWhiteSpace(loaiThayDoi))
            {
                query = query.Where(l => l.LoaiThayDoi == loaiThayDoi);
                ViewData["SelectedLoai"] = loaiThayDoi;
            }

            var list = await query.OrderByDescending(l => l.NgayTao ?? DateTime.MinValue).ToListAsync();
            return View(list);
        }
    }
}
