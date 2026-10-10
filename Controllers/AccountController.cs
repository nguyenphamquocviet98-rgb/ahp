using code.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;
using code.Models.ViewModels;

namespace code.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordService _passwords;
        private readonly IWebHostEnvironment _environment;

        public AccountController(ApplicationDbContext context, PasswordService passwords, IWebHostEnvironment environment)
        {
            _context = context;
            _passwords = passwords;
            _environment = environment;
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
                .Include(u => u.DoanhNghiep)
                .FirstOrDefaultAsync(u => u.TenDangNhap == model.TenDangNhap);

            bool needsUpgrade = false;
            bool isPasswordMatch = user != null && _passwords.Verify(user, model.MatKhau, out needsUpgrade);

            if (user == null || !isPasswordMatch)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
                return View(model);
            }

            if (user.TrangThai != true || (user.MaDoanhNghiep.HasValue && user.DoanhNghiep?.TrangThai != true))
            {
                ModelState.AddModelError(string.Empty, "Tài khoản này hiện đang bị khóa.");
                return View(model);
            }

            if (needsUpgrade)
            {
                user.MatKhau = _passwords.Hash(user, model.MatKhau);
                await _context.SaveChangesAsync();
            }
            await SignInUser(user, model.GhiNhoDangNhap);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            if (user.VaiTro == "admin_san" || user.VaiTro == "admin")
            {
                return RedirectToAction("Index", "DashboardAdmin");
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Đăng ký tài khoản NPP mới (tạo cả Doanh nghiệp + Tài khoản chủ NPP)
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra tên đăng nhập đã tồn tại
            if (await _context.NguoiDungs.AnyAsync(u => u.TenDangNhap == model.TenDangNhap))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập này đã được sử dụng.");
                return View(model);
            }

            // Tạo Doanh nghiệp mới
            var doanhNghiep = new DoanhNghiep
            {
                TenDoanhNghiep = model.TenDoanhNghiep,
                MaSoThue = model.MaSoThue,
                DiaChi = model.DiaChi,
                SoDienThoai = model.SoDienThoaiDN,
                Email = model.EmailDN,
                MoTa = model.MoTa,
                NgayTao = DateTime.Now,
                TrangThai = true,
                LaGianHangMall = false,
                TrangThaiMall = "thuong",
                DiemUyTin = 5.0m
            };


            // Tạo tài khoản chủ NPP
            var nguoiDung = new NguoiDung
            {
                TenDangNhap = model.TenDangNhap,

                VaiTro = "chu_npp",
                TrangThai = true,
                NgayTao = DateTime.Now,
                DoanhNghiep = doanhNghiep
            };
            nguoiDung.MatKhau = _passwords.Hash(nguoiDung, model.MatKhau);
            _context.NguoiDungs.Add(nguoiDung);
            await _context.SaveChangesAsync();

            // Đăng nhập luôn sau khi đăng ký
            await SignInUser(nguoiDung, false);

            TempData["SuccessMessage"] = $"Chào mừng {model.TenDoanhNghiep} đã tham gia Sàn B2B!";
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Quick Switch — Chuyển nhanh tài khoản demo (chỉ dùng trong Development)
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickSwitch(int userId)
        {
            if (!_environment.IsDevelopment()) return NotFound();
            var user = await _context.NguoiDungs
                .Include(u => u.DoanhNghiep)
                .FirstOrDefaultAsync(u => u.MaNguoiDung == userId && u.TrangThai == true);

            if (user == null)
            {
                return NotFound();
            }

            if (user.MaDoanhNghiep.HasValue && user.DoanhNghiep?.TrangThai != true) return Forbid();

            // Đăng xuất rồi đăng nhập lại bằng tài khoản mới
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await SignInUser(user, false);

            if (user.VaiTro == "admin_san" || user.VaiTro == "admin")
            {
                return RedirectToAction("Index", "DashboardAdmin");
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// API lấy danh sách tài khoản demo cho Quick Switcher
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetDemoAccounts()
        {
            if (!_environment.IsDevelopment()) return NotFound();
            var accounts = await _context.NguoiDungs
                .Include(u => u.DoanhNghiep)
                .Where(u => u.TrangThai == true)
                .Select(u => new
                {
                    u.MaNguoiDung,
                    u.TenDangNhap,
                    u.VaiTro,
                    TenDoanhNghiep = u.DoanhNghiep != null ? u.DoanhNghiep.TenDoanhNghiep : "Admin Sàn",
                    LaGianHangMall = u.DoanhNghiep != null && u.DoanhNghiep.LaGianHangMall
                })
                .OrderBy(u => u.VaiTro)
                .ThenBy(u => u.TenDangNhap)
                .ToListAsync();

            return Json(accounts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

        // ===== Helper Methods =====

        /// <summary>
        /// Tạo Claims và đăng nhập cookie cho người dùng
        /// </summary>
        private async Task SignInUser(NguoiDung user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString()),
                new Claim(ClaimTypes.Name, user.TenDangNhap ?? string.Empty),
                new Claim(ClaimTypes.Role, user.VaiTro ?? "nhanvien_npp"),
                new Claim("MaNguoiDung", user.MaNguoiDung.ToString()),
                new Claim("VaiTro", user.VaiTro ?? "nhanvien_npp"),
                new Claim("MaDoanhNghiep", user.MaDoanhNghiep?.ToString() ?? "0"),
                new Claim("TenDoanhNghiep", user.DoanhNghiep?.TenDoanhNghiep ?? "Admin Sàn")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }
    }
}
