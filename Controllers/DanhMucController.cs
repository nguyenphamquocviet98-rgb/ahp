using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;

namespace code.Controllers
{
    [Authorize]
    public class DanhMucController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DanhMucController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DanhMuc
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.DanhMucs
                .Include(d => d.SanPhams)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(d => d.TenDanhMuc != null && d.TenDanhMuc.Contains(search.Trim()));
                ViewData["Search"] = search.Trim();
            }

            var list = await query.OrderBy(d => d.MaDanhMuc).ToListAsync();
            return View(list);
        }

        // GET: DanhMuc/Create
        [Authorize(Roles = "admin,quanly")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: DanhMuc/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Create(DanhMuc model)
        {
            if (string.IsNullOrWhiteSpace(model.TenDanhMuc))
            {
                ModelState.AddModelError("TenDanhMuc", "Tên danh mục không được để trống.");
            }

            if (ModelState.IsValid)
            {
                model.TenDanhMuc = model.TenDanhMuc?.Trim();
                model.MoTa = model.MoTa?.Trim();

                _context.DanhMucs.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm danh mục thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: DanhMuc/Edit/5
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var danhMuc = await _context.DanhMucs.FindAsync(id);
            if (danhMuc == null)
            {
                return NotFound();
            }

            return View(danhMuc);
        }

        // POST: DanhMuc/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Edit(int id, DanhMuc model)
        {
            if (id != model.MaDanhMuc)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(model.TenDanhMuc))
            {
                ModelState.AddModelError("TenDanhMuc", "Tên danh mục không được để trống.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.DanhMucs.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    existing.TenDanhMuc = model.TenDanhMuc?.Trim();
                    existing.MoTa = model.MoTa?.Trim();

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.DanhMucs.Any(e => e.MaDanhMuc == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            return View(model);
        }

        // POST: DanhMuc/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Delete(int id)
        {
            var danhMuc = await _context.DanhMucs
                .Include(d => d.SanPhams)
                .FirstOrDefaultAsync(d => d.MaDanhMuc == id);

            if (danhMuc == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra ràng buộc: Nếu danh mục đã có sản phẩm thì chặn xóa
            if (danhMuc.SanPhams.Any())
            {
                TempData["ErrorMessage"] = $"Không thể xóa danh mục '{danhMuc.TenDanhMuc}' vì đang có {danhMuc.SanPhams.Count} sản phẩm thuộc danh mục này.";
                return RedirectToAction(nameof(Index));
            }

            _context.DanhMucs.Remove(danhMuc);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa danh mục '{danhMuc.TenDanhMuc}' thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
