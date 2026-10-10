using Microsoft.AspNetCore.Mvc.Rendering;
using code.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;

namespace code.Controllers
{
    [Authorize(Roles = "admin,admin_san")]
    public class NguoiDungController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordService _passwords;

        public NguoiDungController(ApplicationDbContext context, PasswordService passwords)
        {
            _context = context;
            _passwords = passwords;
        }

        // GET: NguoiDung
        public async Task<IActionResult> Index(string? search, string? role)
        {
            var query = _context.NguoiDungs.Include(u => u.DoanhNghiep).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(u => u.TenDangNhap != null && u.TenDangNhap.Contains(s));
                ViewData["Search"] = s;
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                if (role == "admin" || role == "admin_san")
                {
                    query = query.Where(u => u.VaiTro == "admin" || u.VaiTro == "admin_san");
                }
                else if (role == "quanly" || role == "chu_npp")
                {
                    query = query.Where(u => u.VaiTro == "quanly" || u.VaiTro == "chu_npp");
                }
                else if (role == "nhanvien" || role == "nhanvien_npp")
                {
                    query = query.Where(u => u.VaiTro == "nhanvien" || u.VaiTro == "nhanvien_npp");
                }
                else
                {
                    query = query.Where(u => u.VaiTro == role);
                }
                ViewData["SelectedRole"] = role;
            }

            var users = await query.OrderBy(u => u.MaNguoiDung).ToListAsync();
            return View(users);
        }

        // GET: NguoiDung/Create
        public async Task<IActionResult> Create()
        {
            var model = new NguoiDung
            {
                VaiTro = "nhanvien",
                TrangThai = true,
                NgayTao = DateTime.Now
            };
            await PopulateCompanies();
            return View(model);
        }

        // POST: NguoiDung/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenDangNhap,MatKhau,VaiTro,MaDoanhNghiep")] NguoiDung model)
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

            await ValidateRole(model.VaiTro, model.MaDoanhNghiep);
            if (ModelState.IsValid)
            {
                model.TrangThai = true;
                model.TenDangNhap = model.TenDangNhap?.Trim() ?? string.Empty;
                model.NgayTao = DateTime.Now;

                model.MatKhau = _passwords.Hash(model, model.MatKhau!);
                _context.NguoiDungs.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Tạo tài khoản '{model.TenDangNhap}' thành công!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateCompanies();
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

            await PopulateCompanies();
            user.MatKhau = null;
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

            await ValidateRole(model.VaiTro, existingUser.MaDoanhNghiep);
            if (ModelState.IsValid)
            {
                existingUser.VaiTro = model.VaiTro;
                existingUser.TrangThai = model.TrangThai;

                // Nếu có nhập mật khẩu mới thì cập nhật
                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    existingUser.MatKhau = _passwords.Hash(existingUser, newPassword);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật tài khoản '{existingUser.TenDangNhap}' thành công!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateCompanies();
            return View(model);
        }

        private async Task PopulateCompanies() => ViewBag.Companies = new SelectList(await _context.DoanhNghieps.Where(d => d.TrangThai).ToListAsync(), "MaDoanhNghiep", "TenDoanhNghiep");
        private async Task ValidateRole(string? role, int? companyId)
        {
            if (role is not ("admin" or "admin_san" or "quanly" or "chu_npp" or "nhanvien" or "nhanvien_npp"))
                ModelState.AddModelError("VaiTro", "Vai trò không hợp lệ.");
            if (role is "admin" or "admin_san")
            {
                if (companyId != null) ModelState.AddModelError("MaDoanhNghiep", "Quản trị hệ thống không thuộc doanh nghiệp.");
            }
            else if (companyId == null || !await _context.DoanhNghieps.AnyAsync(d => d.MaDoanhNghiep == companyId && d.TrangThai))
                ModelState.AddModelError("MaDoanhNghiep", "Chọn doanh nghiệp đang hoạt động.");
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
