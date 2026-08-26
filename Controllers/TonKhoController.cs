using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using code.Data;

namespace code.Controllers
{
    [Authorize]
    public class TonKhoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TonKhoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TonKho
        public async Task<IActionResult> Index(string? search, int? maDanhMuc, string? mucTon)
        {
            var query = _context.TonKhos
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DanhMuc)
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DonViTinh)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(t => t.SanPham != null &&
                                         ((t.SanPham.TenSanPham != null && t.SanPham.TenSanPham.Contains(s)) ||
                                          (t.SanPham.MaSanPhamCode != null && t.SanPham.MaSanPhamCode.Contains(s))));
                ViewData["Search"] = s;
            }

            if (maDanhMuc.HasValue && maDanhMuc.Value > 0)
            {
                query = query.Where(t => t.SanPham != null && t.SanPham.MaDanhMuc == maDanhMuc.Value);
                ViewData["SelectedDanhMuc"] = maDanhMuc.Value;
            }

            if (!string.IsNullOrWhiteSpace(mucTon))
            {
                if (mucTon == "thap")
                {
                    query = query.Where(t => (t.SoLuongTon ?? 0) < 20);
                }
                else if (mucTon == "antoan")
                {
                    query = query.Where(t => (t.SoLuongTon ?? 0) >= 20);
                }
                ViewData["SelectedMucTon"] = mucTon;
            }

            ViewBag.DanhMucs = new SelectList(await _context.DanhMucs.OrderBy(d => d.TenDanhMuc).ToListAsync(), "MaDanhMuc", "TenDanhMuc", maDanhMuc);

            var list = await query.OrderBy(t => t.SoLuongTon).ToListAsync();

            // Tính toán tổng quan kho
            ViewBag.TongSoMatHang = await _context.TonKhos.CountAsync();
            ViewBag.TongSoLuongTon = await _context.TonKhos.SumAsync(t => t.SoLuongTon ?? 0);
            ViewBag.SoMatHangTonThap = await _context.TonKhos.CountAsync(t => (t.SoLuongTon ?? 0) < 20);
            ViewBag.TongGiaTriTonKho = await _context.TonKhos
                .Include(t => t.SanPham)
                .SumAsync(t => (decimal)(t.SoLuongTon ?? 0) * (t.SanPham != null ? (t.SanPham.GiaMacDinh ?? 0) : 0));

            return View(list);
        }
    }
}
