using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;

namespace code.Controllers
{
    [Authorize]
    public class DonViTinhController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DonViTinhController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DonViTinh
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.DonViTinhs
                .Include(d => d.SanPhams)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(d => d.TenDonVi != null && d.TenDonVi.Contains(search.Trim()));
                ViewData["Search"] = search.Trim();
            }

            var list = await query.OrderBy(d => d.MaDonViTinh).ToListAsync();
            return View(list);
        }

        // GET: DonViTinh/Create
        [Authorize(Roles = "admin,quanly")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: DonViTinh/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Create(DonViTinh model)
        {
            if (string.IsNullOrWhiteSpace(model.TenDonVi))
            {
                ModelState.AddModelError("TenDonVi", "Tên đơn vị tính không được để trống.");
            }

            if (ModelState.IsValid)
            {
                model.TenDonVi = model.TenDonVi?.Trim();

                _context.DonViTinhs.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm đơn vị tính thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: DonViTinh/Edit/5
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donViTinh = await _context.DonViTinhs.FindAsync(id);
            if (donViTinh == null)
            {
                return NotFound();
            }

            return View(donViTinh);
        }

        // POST: DonViTinh/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Edit(int id, DonViTinh model)
        {
            if (id != model.MaDonViTinh)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(model.TenDonVi))
            {
                ModelState.AddModelError("TenDonVi", "Tên đơn vị tính không được để trống.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.DonViTinhs.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    existing.TenDonVi = model.TenDonVi?.Trim();

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật đơn vị tính thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.DonViTinhs.Any(e => e.MaDonViTinh == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            return View(model);
        }

        // POST: DonViTinh/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Delete(int id)
        {
            var donViTinh = await _context.DonViTinhs
                .Include(d => d.SanPhams)
                .FirstOrDefaultAsync(d => d.MaDonViTinh == id);

            if (donViTinh == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn vị tính cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra ràng buộc: Nếu đang có sản phẩm dùng ĐVT này thì chặn xóa
            if (donViTinh.SanPhams.Any())
            {
                TempData["ErrorMessage"] = $"Không thể xóa đơn vị tính '{donViTinh.TenDonVi}' vì đang có {donViTinh.SanPhams.Count} sản phẩm sử dụng đơn vị tính này.";
                return RedirectToAction(nameof(Index));
            }

            _context.DonViTinhs.Remove(donViTinh);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa đơn vị tính '{donViTinh.TenDonVi}' thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
