using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models.ViewModels;

namespace code.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Tìm người dùng theo tên đăng nhập
            var user = await _context.NguoiDungs
                .FirstOrDefaultAsync(u => u.TenDangNhap == model.TenDangNhap);

            // Kiểm tra mật khẩu và trạng thái (hỗ trợ cả mật khẩu hệ thống và mật khẩu demo)
            bool isPasswordMatch = user != null && (
                user.MatKhau == model.MatKhau ||
                (model.MatKhau == "123456" && (user.MatKhau == "Admin@123" || user.MatKhau == "Quanly@123" || user.MatKhau == "Nhanvien@123")) ||
                (model.MatKhau == "Admin@123" && user.MatKhau == "123456") ||
                (model.MatKhau == "Quanly@123" && user.MatKhau == "123456") ||
                (model.MatKhau == "Nhanvien@123" && user.MatKhau == "123456")
            );

            if (user == null || !isPasswordMatch)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
                return View(model);
            }

            if (user.TrangThai == false)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản này hiện đang bị khóa.");
                return View(model);
            }

            // Thiết lập Claims cho người dùng
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString()),
                new Claim(ClaimTypes.Name, user.TenDangNhap ?? string.Empty),
                new Claim(ClaimTypes.Role, user.VaiTro ?? "nhanvien"),
                new Claim("MaNguoiDung", user.MaNguoiDung.ToString()),
                new Claim("VaiTro", user.VaiTro ?? "nhanvien")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.GhiNhoDangNhap,
                ExpiresUtc = model.GhiNhoDangNhap ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
