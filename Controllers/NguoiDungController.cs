using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;

namespace code.Controllers
{
    [Authorize(Roles = "admin")]
    public class NguoiDungController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NguoiDungController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: NguoiDung
        public async Task<IActionResult> Index(string? search, string? role)
        {
            var query = _context.NguoiDungs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(u => u.TenDangNhap != null && u.TenDangNhap.Contains(s));
                ViewData["Search"] = s;
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.VaiTro == role);
                ViewData["SelectedRole"] = role;
            }

            var users = await query.OrderBy(u => u.MaNguoiDung).ToListAsync();
            return View(users);
        }

        // GET: NguoiDung/Create
        public IActionResult Create()
        {
            var model = new NguoiDung
            {
                VaiTro = "nhanvien",
                TrangThai = true,
                NgayTao = DateTime.Now
            };
            return View(model);
        }

        // POST: NguoiDung/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NguoiDung model)
        {
            if (string.IsNullOrWhiteSpace(model.TenDangNhap))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập không được để trống.");
            }
            else if (await _context.NguoiDungs.AnyAsync(u => u.TenDangNhap == model.TenDangNhap.Trim()))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại trong hệ thống.");
            }

            if (string.IsNullOrWhiteSpace(model.MatKhau))
            {
                ModelState.AddModelError("MatKhau", "Mật khẩu không được để trống.");
            }

            if (ModelState.IsValid)
            {
                model.TenDangNhap = model.TenDangNhap.Trim();
                model.NgayTao = DateTime.Now;

                _context.NguoiDungs.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Tạo tài khoản '{model.TenDangNhap}' thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: NguoiDung/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.NguoiDungs.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: NguoiDung/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NguoiDung model, string? newPassword)
        {
            if (id != model.MaNguoiDung)
            {
                return NotFound();
            }

            var existingUser = await _context.NguoiDungs.FindAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                existingUser.VaiTro = model.VaiTro;
                existingUser.TrangThai = model.TrangThai;

                // Nếu có nhập mật khẩu mới thì cập nhật
                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    existingUser.MatKhau = newPassword.Trim();
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật tài khoản '{existingUser.TenDangNhap}' thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // POST: NguoiDung/ToggleTrangThai
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTrangThai(int id)
        {
            var user = await _context.NguoiDungs.FindAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra không cho tự khóa tài khoản của chính mình
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("MaNguoiDung")?.Value;
            if (int.TryParse(currentUserIdStr, out int currentUserId) && currentUserId == id)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản của chính mình đang đăng nhập!";
                return RedirectToAction(nameof(Index));
            }

            user.TrangThai = !(user.TrangThai ?? false);
            await _context.SaveChangesAsync();

            var statusStr = user.TrangThai == true ? "mở khóa" : "khóa";
            TempData["SuccessMessage"] = $"Đã {statusStr} tài khoản '{user.TenDangNhap}' thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
