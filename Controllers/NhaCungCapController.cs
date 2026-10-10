using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;
using System.IO;

namespace code.Controllers
{
    [Authorize]
    public class NhaCungCapController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public NhaCungCapController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: NhaCungCap
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.NhaCungCaps
                .Include(n => n.PhieuNhaps)
                .Include(n => n.KetQuaXepHangs)
                    .ThenInclude(k => k.KyDanhGia)
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
        public async Task<IActionResult> Create()
        {
            var maxId = await _context.NhaCungCaps.MaxAsync(n => (int?)n.MaNhaCungCap) ?? 0;
            var model = new NhaCungCap 
            { 
                MaNhaCungCapCode = $"NCC{(maxId + 1):D3}",
                TrangThai = true 
            };
            return View(model);
        }

        // POST: NhaCungCap/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Create(NhaCungCap model, IFormFile? hinhAnhUpload)
        {
            if (string.IsNullOrWhiteSpace(model.TenNhaCungCap))
            {
                ModelState.AddModelError("TenNhaCungCap", "Tên nhà cung cấp không được để trống.");
            }

            if (hinhAnhUpload != null && hinhAnhUpload.Length > 0)
            {
                var uploadResult = await ProcessImageUploadAsync(hinhAnhUpload, "suppliers");
                if (uploadResult.Success)
                {
                    model.HinhAnh = uploadResult.FilePath;
                }
                else
                {
                    ModelState.AddModelError("HinhAnh", uploadResult.ErrorMessage ?? "File ảnh không hợp lệ.");
                }
            }

            if (ModelState.IsValid)
            {
                // Tự động sinh mã nếu để trống
                if (string.IsNullOrWhiteSpace(model.MaNhaCungCapCode))
                {
                    var maxId = await _context.NhaCungCaps.MaxAsync(n => (int?)n.MaNhaCungCap) ?? 0;
                    model.MaNhaCungCapCode = $"NCC{(maxId + 1):D3}";
                }
                else
                {
                    model.MaNhaCungCapCode = model.MaNhaCungCapCode.Trim().ToUpper();
                }

                model.TenNhaCungCap = model.TenNhaCungCap?.Trim();
                model.NguoiLienHe = model.NguoiLienHe?.Trim();
                model.SoDienThoai = model.SoDienThoai?.Trim();
                model.Email = model.Email?.Trim();
                model.DiaChi = model.DiaChi?.Trim();
                model.HinhAnh = model.HinhAnh?.Trim();

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
        public async Task<IActionResult> Edit(int id, NhaCungCap model, IFormFile? hinhAnhUpload, bool xoaAnh = false)
        {
            if (id != model.MaNhaCungCap)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(model.TenNhaCungCap))
            {
                ModelState.AddModelError("TenNhaCungCap", "Tên nhà cung cấp không được để trống.");
            }

            string? newImagePath = null;
            if (hinhAnhUpload != null && hinhAnhUpload.Length > 0)
            {
                var uploadResult = await ProcessImageUploadAsync(hinhAnhUpload, "suppliers");
                if (uploadResult.Success)
                {
                    newImagePath = uploadResult.FilePath;
                }
                else
                {
                    ModelState.AddModelError("HinhAnh", uploadResult.ErrorMessage ?? "File ảnh không hợp lệ.");
                }
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

                    if (xoaAnh)
                    {
                        DeleteOldImage(existing.HinhAnh);
                        existing.HinhAnh = null;
                    }
                    else if (newImagePath != null)
                    {
                        DeleteOldImage(existing.HinhAnh);
                        existing.HinhAnh = newImagePath;
                    }
                    else if (!string.IsNullOrWhiteSpace(model.HinhAnh))
                    {
                        existing.HinhAnh = model.HinhAnh.Trim();
                    }

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

            // Kiểm tra ràng buộc: Nếu có dữ liệu liên kết thì chặn xóa
            if (ncc.PhieuNhaps.Any() || ncc.DiemDanhGiaNCCs.Any() || ncc.KetQuaXepHangs.Any())
            {
                TempData["ErrorMessage"] = $"Không thể xóa nhà cung cấp '{ncc.TenNhaCungCap}' vì đã có dữ liệu phiếu nhập hoặc điểm đánh giá liên kết.";
                return RedirectToAction(nameof(Index));
            }

            // Xóa file ảnh đại diện nếu có
            DeleteOldImage(ncc.HinhAnh);

            _context.NhaCungCaps.Remove(ncc);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa nhà cung cấp '{ncc.TenNhaCungCap}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        private async Task<(bool Success, string? FilePath, string? ErrorMessage)> ProcessImageUploadAsync(IFormFile file, string folder)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return (false, null, "Chỉ chấp nhận các định dạng ảnh: .jpg, .jpeg, .png, .webp, .gif");
            }

            // Giới hạn 5MB
            if (file.Length > 5 * 1024 * 1024)
            {
                return (false, null, "Dung lượng ảnh không được vượt quá 5MB.");
            }

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", folder);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return (true, $"/uploads/{folder}/{uniqueFileName}", null);
        }

        private void DeleteOldImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !imagePath.StartsWith("/uploads/"))
            {
                return;
            }

            try
            {
                var relativePath = imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch
            {
                // Bỏ qua lỗi xóa file nếu bị lock
            }
        }
    }
}
