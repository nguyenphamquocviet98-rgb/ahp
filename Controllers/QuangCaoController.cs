using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;
using System.Security.Claims;

namespace code.Controllers
{
    [Authorize]
    public class QuangCaoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuangCaoController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentMaDoanhNghiep()
        {
            var claim = User.FindFirstValue("MaDoanhNghiep");
            int.TryParse(claim, out int dnId);
            return dnId;
        }

        /// <summary>
        /// Danh sách Banner quảng cáo trên sàn
        /// </summary>
        public async Task<IActionResult> Index()
        {
            int currentDNId = GetCurrentMaDoanhNghiep();
            bool isAdmin = User.IsInRole("admin_san") || currentDNId == 0;

            var query = _context.QuangCaoBanners
                .Include(b => b.DoanhNghiep)
                .AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(b => b.MaDoanhNghiep == currentDNId);
            }

            var list = await query.OrderByDescending(b => b.NgayTao).ToListAsync();

            ViewBag.IsAdmin = isAdmin;
            ViewBag.CurrentDNId = currentDNId;
            return View(list);
        }

        /// <summary>
        /// NPP: Đăng ký đặt Banner tài trợ trên trang nhất
        /// </summary>
        [HttpGet]
        public IActionResult DangKy()
        {
            int currentDNId = GetCurrentMaDoanhNghiep();
            if (currentDNId <= 0)
            {
                TempData["ErrorMessage"] = "Bạn cần là Nhà phân phối để đăng ký đặt banner tài trợ!";
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangKy(string tieuDe, string? lienKet, int thoiHanTuan)
        {
            int currentDNId = GetCurrentMaDoanhNghiep();
            if (currentDNId <= 0)
            {
                TempData["ErrorMessage"] = "Bạn cần là Nhà phân phối để đăng ký đặt banner!";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(tieuDe))
            {
                ModelState.AddModelError("", "Tiêu đề quảng cáo không được để trống!");
                return View();
            }

            // Tính chi phí: 1 tuần = 1.000.000đ, 2 tuần = 1.800.000đ, 4 tuần (1 tháng) = 3.000.000đ
            decimal chiPhi = thoiHanTuan switch
            {
                1 => 1000000,
                2 => 1800000,
                4 => 3000000,
                _ => thoiHanTuan * 800000
            };

            var banner = new QuangCaoBanner
            {
                MaDoanhNghiep = currentDNId,
                TieuDe = tieuDe,
                LienKet = string.IsNullOrWhiteSpace(lienKet) ? "/SanB2B" : lienKet,
                ViTri = "trang_nhat",
                NgayBatDau = DateTime.Now,
                NgayKetThuc = DateTime.Now.AddDays(thoiHanTuan * 7),
                ChiPhi = chiPhi,
                TrangThai = "cho_duyet",
                NgayTao = DateTime.Now
            };

            _context.QuangCaoBanners.Add(banner);
            await _context.SaveChangesAsync();

            // Ghi nhận vào sổ cái Doanh thu Sàn của Admin
            var dn = await _context.DoanhNghieps.FindAsync(currentDNId);
            _context.DoanhThuSans.Add(new DoanhThuSan
            {
                MaDoanhNghiep = currentDNId,
                LoaiThu = "phi_quang_cao",
                SoTien = chiPhi,
                NgayGiaoDich = DateTime.Now,
                MoTa = $"Phí tài trợ Banner trang nhất ({thoiHanTuan} tuần) của {dn?.TenDoanhNghiep}",
                TrangThai = "da_thu"
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đăng ký Banner tài trợ thành công! Yêu cầu đang được Admin sàn xét duyệt kích hoạt.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Admin: Duyệt hoặc Từ chối Banner quảng cáo
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin_san")]
        public async Task<IActionResult> Duyet(int id, bool approve)
        {
            var banner = await _context.QuangCaoBanners.FindAsync(id);
            if (banner == null) return NotFound();

            banner.TrangThai = approve ? "hoat_dong" : "tu_choi";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = approve ? "Đã duyệt và hiển thị Banner lên trang nhất!" : "Đã từ chối Banner quảng cáo.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Admin hoặc chủ banner xóa banner
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Xoa(int id)
        {
            var banner = await _context.QuangCaoBanners.FindAsync(id);
            if (banner == null) return NotFound();

            _context.QuangCaoBanners.Remove(banner);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa banner quảng cáo thành công.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// NPP: Trang Mua Gói Gian Hàng Mall & Dịch vụ Quảng Cáo B2B
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MuaGoi()
        {
            int currentDNId = GetCurrentMaDoanhNghiep();
            bool isAdmin = User.IsInRole("admin_san") || User.IsInRole("admin");

            DoanhNghiep? dn = null;
            if (currentDNId > 0)
            {
                dn = await _context.DoanhNghieps.FirstOrDefaultAsync(d => d.MaDoanhNghiep == currentDNId);
            }
            else if (isAdmin)
            {
                // Nếu Admin ghé xem, lấy doanh nghiệp đầu tiên để preview giao diện
                dn = await _context.DoanhNghieps.FirstOrDefaultAsync();
            }

            int targetDNId = dn?.MaDoanhNghiep ?? currentDNId;

            // Lịch sử giao dịch dịch vụ sàn của DN
            var lichSuGiaoDich = await _context.DoanhThuSans
                .Include(d => d.DoanhNghiep)
                .Where(d => targetDNId == 0 || d.MaDoanhNghiep == targetDNId)
                .OrderByDescending(d => d.NgayGiaoDich)
                .Take(25)
                .ToListAsync();

            // Danh sách banner của DN
            var myBanners = await _context.QuangCaoBanners
                .Where(b => targetDNId == 0 || b.MaDoanhNghiep == targetDNId)
                .OrderByDescending(b => b.NgayTao)
                .ToListAsync();

            ViewBag.IsAdmin = isAdmin;
            ViewBag.DoanhNghiep = dn;
            ViewBag.LichSuGiaoDich = lichSuGiaoDich;
            ViewBag.MyBanners = myBanners;

            return View(dn);
        }

        /// <summary>
        /// NPP: Đăng ký / Gia hạn Gói Gian Hàng Mall
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MuaGoiMall(int thoiHanThang, string? giayPhepKinhDoanh, string? ghiChu, string phuongThuc = "chuyen_khoan")
        {
            int currentDNId = GetCurrentMaDoanhNghiep();
            bool isAdmin = User.IsInRole("admin_san") || User.IsInRole("admin");

            if (currentDNId <= 0 && !isAdmin)
            {
                TempData["ErrorMessage"] = "Bạn cần là Nhà phân phối để đăng ký Gói Mall!";
                return RedirectToAction("MuaGoi");
            }

            // Nếu Admin test thử, lấy DN đầu tiên
            var dn = currentDNId > 0 
                ? await _context.DoanhNghieps.FindAsync(currentDNId)
                : await _context.DoanhNghieps.FirstOrDefaultAsync();

            if (dn == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin Doanh nghiệp!";
                return RedirectToAction("MuaGoi");
            }

            // Tính chi phí: 6 tháng = 4.990.000đ, 12 tháng = 8.990.000đ, 24 tháng = 15.990.000đ
            decimal chiPhi = thoiHanThang switch
            {
                6 => 4990000,
                12 => 8990000,
                24 => 15990000,
                _ => thoiHanThang * 800000
            };

            // Kích hoạt hoặc gia hạn hạn sử dụng Mall
            DateTime ngayBatDauMoi = (dn.LaGianHangMall && dn.HanMall.HasValue && dn.HanMall.Value > DateTime.Now)
                ? dn.HanMall.Value
                : DateTime.Now;

            dn.LaGianHangMall = true;
            dn.TrangThaiMall = "mall";
            dn.NgayThamGiaMall ??= DateTime.Now;
            dn.HanMall = ngayBatDauMoi.AddMonths(thoiHanThang);

            if (!string.IsNullOrWhiteSpace(giayPhepKinhDoanh))
            {
                dn.GiayPhepKinhDoanh = giayPhepKinhDoanh.Trim();
            }

            // Tăng nhẹ điểm uy tín khi tham gia Mall (+0.5đ uy tín hệ thống)
            if (dn.DiemUyTin < 9.5m)
            {
                dn.DiemUyTin = Math.Min(10.0m, dn.DiemUyTin + 0.5m);
            }

            // Ghi nhận vào doanh thu sàn của Admin
            var gd = new DoanhThuSan
            {
                MaDoanhNghiep = dn.MaDoanhNghiep,
                LoaiThu = "phi_mall",
                SoTien = chiPhi,
                NgayGiaoDich = DateTime.Now,
                MoTa = $"Đăng ký Gói Mall {thoiHanThang} tháng ({dn.TenDoanhNghiep} - PTTT: {phuongThuc})",
                TrangThai = "da_thu"
            };
            _context.DoanhThuSans.Add(gd);

            // Nếu gói 12 hoặc 24 tháng: Tự động tặng 1 chiến dịch banner trang nhất
            if (thoiHanThang >= 12)
            {
                int tuanTang = thoiHanThang >= 24 ? 4 : 1;
                var bannerTang = new QuangCaoBanner
                {
                    MaDoanhNghiep = dn.MaDoanhNghiep,
                    TieuDe = $"[MALL CHÍNH HÃNG] {dn.TenDoanhNghiep} - Tổng kho phân phối uy tín cam kết chất lượng",
                    LienKet = $"/SanB2B?search={Uri.EscapeDataString(dn.TenDoanhNghiep)}",
                    ViTri = "trang_nhat",
                    NgayBatDau = DateTime.Now,
                    NgayKetThuc = DateTime.Now.AddDays(tuanTang * 7),
                    ChiPhi = 0,
                    TrangThai = "hoat_dong",
                    NgayTao = DateTime.Now
                };
                _context.QuangCaoBanners.Add(bannerTang);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"🎉 Chúc mừng! Gian hàng '{dn.TenDoanhNghiep}' đã kích hoạt thành công Gói Mall ({thoiHanThang} tháng). Hạn sử dụng đến {dn.HanMall?.ToString("dd/MM/yyyy")}!";
            return RedirectToAction("MuaGoi");
        }

        /// <summary>
        /// NPP: Đăng ký Banner tài trợ trực tiếp từ trang Mua Gói
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MuaGoiBanner(string tieuDe, string? lienKet, int thoiHanTuan, string phuongThuc = "chuyen_khoan")
        {
            int currentDNId = GetCurrentMaDoanhNghiep();
            bool isAdmin = User.IsInRole("admin_san") || User.IsInRole("admin");

            if (currentDNId <= 0 && !isAdmin)
            {
                TempData["ErrorMessage"] = "Bạn cần là Nhà phân phối để đăng ký Banner!";
                return RedirectToAction("MuaGoi");
            }

            var dn = currentDNId > 0 
                ? await _context.DoanhNghieps.FindAsync(currentDNId)
                : await _context.DoanhNghieps.FirstOrDefaultAsync();

            if (dn == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin Doanh nghiệp!";
                return RedirectToAction("MuaGoi");
            }

            if (string.IsNullOrWhiteSpace(tieuDe))
            {
                TempData["ErrorMessage"] = "Tiêu đề chiến dịch Banner không được để trống!";
                return RedirectToAction("MuaGoi");
            }

            decimal chiPhi = thoiHanTuan switch
            {
                1 => 1000000,
                2 => 1800000,
                4 => 3000000,
                _ => thoiHanTuan * 800000
            };

            var banner = new QuangCaoBanner
            {
                MaDoanhNghiep = dn.MaDoanhNghiep,
                TieuDe = tieuDe.Trim(),
                LienKet = string.IsNullOrWhiteSpace(lienKet) ? $"/SanB2B?search={Uri.EscapeDataString(dn.TenDoanhNghiep)}" : lienKet.Trim(),
                ViTri = "trang_nhat",
                NgayBatDau = DateTime.Now,
                NgayKetThuc = DateTime.Now.AddDays(thoiHanTuan * 7),
                ChiPhi = chiPhi,
                TrangThai = "hoat_dong", // Tự động kích hoạt khi thanh toán thành công
                NgayTao = DateTime.Now
            };

            _context.QuangCaoBanners.Add(banner);

            _context.DoanhThuSans.Add(new DoanhThuSan
            {
                MaDoanhNghiep = dn.MaDoanhNghiep,
                LoaiThu = "phi_quang_cao",
                SoTien = chiPhi,
                NgayGiaoDich = DateTime.Now,
                MoTa = $"Phí tài trợ Banner trang nhất ({thoiHanTuan} tuần) - {dn.TenDoanhNghiep} (PTTT: {phuongThuc})",
                TrangThai = "da_thu"
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"🚀 Đăng ký Banner tài trợ thành công! Banner chiến dịch '{tieuDe}' đã được kích hoạt trực tiếp trên trang nhất Chợ Sỉ B2B.";
            return RedirectToAction("MuaGoi");
        }
    }
}
