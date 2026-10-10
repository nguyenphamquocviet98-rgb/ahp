using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;

namespace code.Controllers
{
    [Authorize(Roles = "admin,admin_san")]
    public class DoanhNghiepController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoanhNghiepController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Danh sách tất cả Doanh nghiệp NPP trên sàn
        /// </summary>
        public async Task<IActionResult> Index(string? search, string? trangThaiMall)
        {
            var query = _context.DoanhNghieps.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(d =>
                    d.TenDoanhNghiep.Contains(search) ||
                    (d.MaSoThue != null && d.MaSoThue.Contains(search)) ||
                    (d.Email != null && d.Email.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(trangThaiMall))
            {
                query = query.Where(d => d.TrangThaiMall == trangThaiMall);
            }

            var list = await query.OrderByDescending(d => d.NgayTao).ToListAsync();

            // Thống kê
            ViewBag.TongDN = await _context.DoanhNghieps.CountAsync();
            ViewBag.TongMall = await _context.DoanhNghieps.CountAsync(d => d.LaGianHangMall);
            ViewBag.TongDangKyMall = await _context.DoanhNghieps.CountAsync(d => d.TrangThaiMall == "dangky");
            ViewBag.Search = search;
            ViewBag.TrangThaiMall = trangThaiMall;

            return View(list);
        }

        /// <summary>
        /// Chi tiết doanh nghiệp
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var dn = await _context.DoanhNghieps
                .Include(d => d.NguoiDungs)
                .Include(d => d.TonKhoNPPs)
                    .ThenInclude(t => t.SanPham)
                .FirstOrDefaultAsync(d => d.MaDoanhNghiep == id);

            if (dn == null) return NotFound();

            ViewBag.DanhGias = await _context.DanhGiaNPPs
                .Include(dg => dg.NPPMua)
                .Where(dg => dg.MaNPPBan == id)
                .OrderByDescending(dg => dg.NgayDanhGia)
                .ToListAsync();

            return View(dn);
        }

        /// <summary>
        /// Sửa thông tin doanh nghiệp
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var dn = await _context.DoanhNghieps.FindAsync(id);
            if (dn == null) return NotFound();
            return View(dn);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DoanhNghiep model)
        {
            var dn = await _context.DoanhNghieps.FindAsync(id);
            if (dn == null) return NotFound();

            dn.TenDoanhNghiep = model.TenDoanhNghiep;
            dn.MaSoThue = model.MaSoThue;
            dn.DiaChi = model.DiaChi;
            dn.SoDienThoai = model.SoDienThoai;
            dn.Email = model.Email;
            dn.MoTa = model.MoTa;
            dn.TrangThai = model.TrangThai;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật thông tin doanh nghiệp thành công!";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Duyệt / Từ chối yêu cầu Mall
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DuyetMall(int id, bool approve)
        {
            var dn = await _context.DoanhNghieps.FindAsync(id);
            if (dn == null) return NotFound();

            if (approve)
            {
                dn.LaGianHangMall = true;
                dn.TrangThaiMall = "mall";
                dn.NgayThamGiaMall = DateTime.Now;
                dn.HanMall = DateTime.Now.AddYears(1);
                TempData["SuccessMessage"] = $"Đã duyệt Gian hàng Mall cho {dn.TenDoanhNghiep}!";
            }
            else
            {
                dn.TrangThaiMall = "thuong";
                dn.LaGianHangMall = false;
                TempData["SuccessMessage"] = $"Đã từ chối yêu cầu Mall của {dn.TenDoanhNghiep}.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Khóa / Mở khóa doanh nghiệp
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ToggleTrangThai(int id)
        {
            var dn = await _context.DoanhNghieps.FindAsync(id);
            if (dn == null) return NotFound();

            dn.TrangThai = !dn.TrangThai;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = dn.TrangThai
                ? $"Đã kích hoạt lại {dn.TenDoanhNghiep}."
                : $"Đã khóa {dn.TenDoanhNghiep}.";
            return RedirectToAction("Index");
        }
    }
}
