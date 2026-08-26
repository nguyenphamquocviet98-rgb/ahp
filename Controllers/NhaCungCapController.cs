using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;

namespace code.Controllers
{
    [Authorize]
    public class NhaCungCapController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NhaCungCapController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: NhaCungCap
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.NhaCungCaps
                .Include(n => n.PhieuNhaps)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(n => (n.TenNhaCungCap != null && n.TenNhaCungCap.Contains(s)) ||
                                         (n.MaNhaCungCapCode != null && n.MaNhaCungCapCode.Contains(s)) ||
                                         (n.NguoiLienHe != null && n.NguoiLienHe.Contains(s)) ||
                                         (n.SoDienThoai != null && n.SoDienThoai.Contains(s)));
                ViewData["Search"] = s;
            }

            var list = await query.OrderBy(n => n.MaNhaCungCap).ToListAsync();
            return View(list);
        }

        // GET: NhaCungCap/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ncc = await _context.NhaCungCaps
                .Include(n => n.PhieuNhaps)
                    .ThenInclude(p => p.ChiTietPhieuNhaps)
                .Include(n => n.KetQuaXepHangs)
                    .ThenInclude(k => k.KyDanhGia)
                .Include(n => n.DiemDanhGiaNCCs)
                    .ThenInclude(d => d.TieuChi)
                .FirstOrDefaultAsync(m => m.MaNhaCungCap == id);

            if (ncc == null)
            {
                return NotFound();
            }

            return View(ncc);
        }

        // GET: NhaCungCap/Create
        [Authorize(Roles = "admin,quanly")]
        public IActionResult Create()
        {
            var model = new NhaCungCap { TrangThai = true };
            return View(model);
        }

        // POST: NhaCungCap/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Create(NhaCungCap model)
        {
            if (string.IsNullOrWhiteSpace(model.TenNhaCungCap))
            {
                ModelState.AddModelError("TenNhaCungCap", "Tên nhà cung cấp không được để trống.");
            }

            if (ModelState.IsValid)
            {
                model.MaNhaCungCapCode = model.MaNhaCungCapCode?.Trim().ToUpper();
                model.TenNhaCungCap = model.TenNhaCungCap?.Trim();
                model.NguoiLienHe = model.NguoiLienHe?.Trim();
                model.SoDienThoai = model.SoDienThoai?.Trim();
                model.Email = model.Email?.Trim();
                model.DiaChi = model.DiaChi?.Trim();

                _context.NhaCungCaps.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Thêm nhà cung cấp '{model.TenNhaCungCap}' thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: NhaCungCap/Edit/5
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ncc = await _context.NhaCungCaps.FindAsync(id);
            if (ncc == null)
            {
                return NotFound();
            }

            return View(ncc);
        }

        // POST: NhaCungCap/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Edit(int id, NhaCungCap model)
        {
            if (id != model.MaNhaCungCap)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(model.TenNhaCungCap))
            {
                ModelState.AddModelError("TenNhaCungCap", "Tên nhà cung cấp không được để trống.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.NhaCungCaps.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    existing.MaNhaCungCapCode = model.MaNhaCungCapCode?.Trim().ToUpper();
                    existing.TenNhaCungCap = model.TenNhaCungCap?.Trim();
                    existing.NguoiLienHe = model.NguoiLienHe?.Trim();
                    existing.SoDienThoai = model.SoDienThoai?.Trim();
                    existing.Email = model.Email?.Trim();
                    existing.DiaChi = model.DiaChi?.Trim();
                    existing.TrangThai = model.TrangThai;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Cập nhật nhà cung cấp '{model.TenNhaCungCap}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.NhaCungCaps.Any(e => e.MaNhaCungCap == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            return View(model);
        }

        // POST: NhaCungCap/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Delete(int id)
        {
            var ncc = await _context.NhaCungCaps
                .Include(n => n.PhieuNhaps)
                .Include(n => n.DiemDanhGiaNCCs)
                .Include(n => n.KetQuaXepHangs)
                .FirstOrDefaultAsync(n => n.MaNhaCungCap == id);

            if (ncc == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhà cung cấp cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra ràng buộc an toàn
            if (ncc.PhieuNhaps.Any())
            {
                TempData["ErrorMessage"] = $"Không thể xóa nhà cung cấp '{ncc.TenNhaCungCap}' vì đã có {ncc.PhieuNhaps.Count} phiếu nhập hàng liên kết.";
                return RedirectToAction(nameof(Index));
            }

            if (ncc.DiemDanhGiaNCCs.Any() || ncc.KetQuaXepHangs.Any())
            {
                TempData["ErrorMessage"] = $"Không thể xóa nhà cung cấp '{ncc.TenNhaCungCap}' vì đang có dữ liệu trong các kỳ đánh giá AHP.";
                return RedirectToAction(nameof(Index));
            }

            _context.NhaCungCaps.Remove(ncc);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa nhà cung cấp '{ncc.TenNhaCungCap}' thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
