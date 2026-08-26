using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;

namespace code.Controllers
{
    [Authorize]
    public class SanPhamController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SanPhamController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: SanPham
        public async Task<IActionResult> Index(string? search, int? maDanhMuc)
        {
            var query = _context.SanPhams
                .Include(s => s.DanhMuc)
                .Include(s => s.DonViTinh)
                .Include(s => s.TonKho)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(p => (p.TenSanPham != null && p.TenSanPham.Contains(s)) ||
                                         (p.MaSanPhamCode != null && p.MaSanPhamCode.Contains(s)));
                ViewData["Search"] = s;
            }

            if (maDanhMuc.HasValue && maDanhMuc.Value > 0)
            {
                query = query.Where(p => p.MaDanhMuc == maDanhMuc.Value);
                ViewData["SelectedDanhMuc"] = maDanhMuc.Value;
            }

            ViewBag.DanhMucs = new SelectList(await _context.DanhMucs.OrderBy(d => d.TenDanhMuc).ToListAsync(), "MaDanhMuc", "TenDanhMuc", maDanhMuc);

            var list = await query.OrderBy(s => s.MaSanPham).ToListAsync();
            return View(list);
        }

        // GET: SanPham/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams
                .Include(s => s.DanhMuc)
                .Include(s => s.DonViTinh)
                .Include(s => s.TonKho)
                .Include(s => s.ChiTietPhieuNhaps)
                    .ThenInclude(c => c.PhieuNhap)
                        .ThenInclude(p => p!.NhaCungCap)
                .FirstOrDefaultAsync(m => m.MaSanPham == id);

            if (sanPham == null)
            {
                return NotFound();
            }

            return View(sanPham);
        }

        // GET: SanPham/Create
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdownsAsync();
            var model = new SanPham { TrangThai = true };
            return View(model);
        }

        // POST: SanPham/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Create(SanPham model)
        {
            if (string.IsNullOrWhiteSpace(model.TenSanPham))
            {
                ModelState.AddModelError("TenSanPham", "Tên sản phẩm không được để trống.");
            }

            if (ModelState.IsValid)
            {
                model.MaSanPhamCode = model.MaSanPhamCode?.Trim().ToUpper();
                model.TenSanPham = model.TenSanPham?.Trim();
                model.AnhSanPham = model.AnhSanPham?.Trim();

                _context.SanPhams.Add(model);
                await _context.SaveChangesAsync();

                // Tự động khởi tạo 1 bản ghi TonKho (SoLuongTon = 0) theo quan hệ 1-1
                var tonKho = new TonKho
                {
                    MaSanPham = model.MaSanPham,
                    SoLuongTon = 0,
                    NgayCapNhat = DateTime.Now
                };
                _context.TonKhos.Add(tonKho);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Thêm sản phẩm '{model.TenSanPham}' thành công!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(model.MaDanhMuc, model.MaDonViTinh);
            return View(model);
        }

        // GET: SanPham/Edit/5
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams.FindAsync(id);
            if (sanPham == null)
            {
                return NotFound();
            }

            await PopulateDropdownsAsync(sanPham.MaDanhMuc, sanPham.MaDonViTinh);
            return View(sanPham);
        }

        // POST: SanPham/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Edit(int id, SanPham model)
        {
            if (id != model.MaSanPham)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(model.TenSanPham))
            {
                ModelState.AddModelError("TenSanPham", "Tên sản phẩm không được để trống.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.SanPhams.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    existing.MaSanPhamCode = model.MaSanPhamCode?.Trim().ToUpper();
                    existing.TenSanPham = model.TenSanPham?.Trim();
                    existing.MaDanhMuc = model.MaDanhMuc;
                    existing.MaDonViTinh = model.MaDonViTinh;
                    existing.GiaMacDinh = model.GiaMacDinh;
                    existing.AnhSanPham = model.AnhSanPham?.Trim();
                    existing.TrangThai = model.TrangThai;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Cập nhật sản phẩm '{model.TenSanPham}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.SanPhams.Any(e => e.MaSanPham == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            await PopulateDropdownsAsync(model.MaDanhMuc, model.MaDonViTinh);
            return View(model);
        }

        // POST: SanPham/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Delete(int id)
        {
            var sanPham = await _context.SanPhams
                .Include(s => s.TonKho)
                .Include(s => s.ChiTietPhieuNhaps)
                .Include(s => s.LichSuNhapHangs)
                .FirstOrDefaultAsync(s => s.MaSanPham == id);

            if (sanPham == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra ràng buộc: Nếu đã có lịch sử nhập hàng hoặc chi tiết phiếu nhập thì chặn xóa
            if (sanPham.ChiTietPhieuNhaps.Any() || sanPham.LichSuNhapHangs.Any())
            {
                TempData["ErrorMessage"] = $"Không thể xóa sản phẩm '{sanPham.TenSanPham}' vì đã có dữ liệu giao dịch phiếu nhập hàng liên kết.";
                return RedirectToAction(nameof(Index));
            }

            // Xóa bản ghi TonKho liên kết trước nếu có
            if (sanPham.TonKho != null)
            {
                _context.TonKhos.Remove(sanPham.TonKho);
            }

            _context.SanPhams.Remove(sanPham);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa sản phẩm '{sanPham.TenSanPham}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(int? selectedDanhMuc = null, int? selectedDonViTinh = null)
        {
            ViewBag.DanhMucs = new SelectList(
                await _context.DanhMucs.OrderBy(d => d.TenDanhMuc).ToListAsync(),
                "MaDanhMuc",
                "TenDanhMuc",
                selectedDanhMuc);

            ViewBag.DonViTinhs = new SelectList(
                await _context.DonViTinhs.OrderBy(d => d.TenDonVi).ToListAsync(),
                "MaDonViTinh",
                "TenDonVi",
                selectedDonViTinh);
        }
    }
}
