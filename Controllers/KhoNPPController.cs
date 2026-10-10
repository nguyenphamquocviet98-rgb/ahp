using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;

namespace code.Controllers
{
    [Authorize(Roles = "chu_npp,nhanvien_npp,quanly,nhanvien")]
    public class KhoNPPController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KhoNPPController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy MaDoanhNghiep từ Claims của user đang đăng nhập
        /// </summary>
        private int GetCurrentDoanhNghiepId()
        {
            var claim = User.FindFirst("MaDoanhNghiep");
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        /// <summary>
        /// Trang quản lý kho hàng của NPP hiện tại
        /// </summary>
        public async Task<IActionResult> Index(string? search, string? trangThai)
        {
            var maDoanhNghiep = GetCurrentDoanhNghiepId();
            if (maDoanhNghiep == 0) return RedirectToAction("Login", "Account");

            var query = _context.TonKhoNPPs
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DanhMuc)
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DonViTinh)
                .Where(t => t.MaDoanhNghiep == maDoanhNghiep);

            // Lọc theo từ khóa
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t =>
                    (t.SanPham != null && t.SanPham.TenSanPham != null && t.SanPham.TenSanPham.Contains(search)) ||
                    (t.SanPham != null && t.SanPham.MaSanPhamCode != null && t.SanPham.MaSanPhamCode.Contains(search)));
            }

            // Lọc theo trạng thái chào bán
            if (trangThai == "dangban")
            {
                query = query.Where(t => t.DangChaoBan == true);
            }
            else if (trangThai == "chuaban")
            {
                query = query.Where(t => t.DangChaoBan == false);
            }

            var list = await query.OrderBy(t => t.SanPham!.TenSanPham).ToListAsync();

            // Lấy thông tin doanh nghiệp hiện tại
            var doanhNghiep = await _context.DoanhNghieps.FindAsync(maDoanhNghiep);
            ViewBag.DoanhNghiep = doanhNghiep;
            ViewBag.Search = search;
            ViewBag.TrangThai = trangThai;

            return View(list);
        }

        /// <summary>
        /// Form thêm sản phẩm vào kho NPP
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ThemSanPham()
        {
            var maDoanhNghiep = GetCurrentDoanhNghiepId();
            if (maDoanhNghiep == 0) return RedirectToAction("Login", "Account");

            // Lấy danh sách sản phẩm chưa có trong kho NPP này
            var daCo = await _context.TonKhoNPPs
                .Where(t => t.MaDoanhNghiep == maDoanhNghiep)
                .Select(t => t.MaSanPham)
                .ToListAsync();

            var sanPhams = await _context.SanPhams
                .Include(s => s.DonViTinh)
                .Where(s => s.TrangThai == true && !daCo.Contains(s.MaSanPham))
                .ToListAsync();

            ViewBag.SanPhams = new SelectList(sanPhams, "MaSanPham", "TenSanPham");
            return View(new TonKhoNPP { MaDoanhNghiep = maDoanhNghiep });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemSanPham([Bind("MaSanPham,GiaSi,ThoiGianGiaoHang,DangChaoBan")] TonKhoNPP model)
        {
            var maDoanhNghiep = GetCurrentDoanhNghiepId();
            if (maDoanhNghiep == 0) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid || model.GiaSi < 0 || model.ThoiGianGiaoHang <= 0 || (model.DangChaoBan && model.GiaSi <= 0)) return BadRequest("Giá và thời gian giao hàng không hợp lệ.");
            model.MaTonKhoNPP = 0;
            model.SoLuongTon = 0;
            model.SanPham = null;
            model.DoanhNghiep = null;
            model.MaDoanhNghiep = maDoanhNghiep;
            model.NgayCapNhat = DateTime.Now;

            // Kiểm tra đã tồn tại chưa
            var existing = await _context.TonKhoNPPs
                .FirstOrDefaultAsync(t => t.MaDoanhNghiep == maDoanhNghiep && t.MaSanPham == model.MaSanPham);

            if (existing != null)
            {
                TempData["ErrorMessage"] = "Sản phẩm này đã có trong kho của bạn.";
                return RedirectToAction("Index");
            }

            _context.TonKhoNPPs.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã thêm sản phẩm vào kho thành công!";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Sửa thông tin kho: Giá sỉ, Tồn kho, Thời gian giao, Trạng thái chào bán
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var maDoanhNghiep = GetCurrentDoanhNghiepId();
            var tonKho = await _context.TonKhoNPPs
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DonViTinh)
                .FirstOrDefaultAsync(t => t.MaTonKhoNPP == id && t.MaDoanhNghiep == maDoanhNghiep);

            if (tonKho == null) return NotFound();

            return View(tonKho);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TonKhoNPP model)
        {
            var maDoanhNghiep = GetCurrentDoanhNghiepId();
            var tonKho = await _context.TonKhoNPPs
                .FirstOrDefaultAsync(t => t.MaTonKhoNPP == id && t.MaDoanhNghiep == maDoanhNghiep);

            if (tonKho == null) return NotFound();

            if (!ModelState.IsValid || model.GiaSi < 0 || model.ThoiGianGiaoHang <= 0 || (model.DangChaoBan && model.GiaSi <= 0)) return BadRequest("Giá và thời gian giao hàng không hợp lệ.");
            // Tồn kho chỉ thay đổi qua chứng từ nhập hàng và đơn mua/bán.
            tonKho.GiaSi = model.GiaSi;
            tonKho.ThoiGianGiaoHang = model.ThoiGianGiaoHang;
            tonKho.DangChaoBan = model.DangChaoBan;
            tonKho.NgayCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật kho hàng thành công!";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Bật/Tắt chào bán nhanh (AJAX)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleChaoBan(int id)
        {
            var maDoanhNghiep = GetCurrentDoanhNghiepId();
            var tonKho = await _context.TonKhoNPPs
                .FirstOrDefaultAsync(t => t.MaTonKhoNPP == id && t.MaDoanhNghiep == maDoanhNghiep);

            if (tonKho == null) return NotFound();

            if (!tonKho.DangChaoBan && (tonKho.GiaSi <= 0 || tonKho.ThoiGianGiaoHang <= 0)) return BadRequest("Cần giá bán và thời gian giao hợp lệ.");
            tonKho.DangChaoBan = !tonKho.DangChaoBan;
            tonKho.NgayCapNhat = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, dangChaoBan = tonKho.DangChaoBan });
        }

        /// <summary>
        /// Xóa sản phẩm khỏi kho NPP
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var maDoanhNghiep = GetCurrentDoanhNghiepId();
            var tonKho = await _context.TonKhoNPPs
                .FirstOrDefaultAsync(t => t.MaTonKhoNPP == id && t.MaDoanhNghiep == maDoanhNghiep);

            if (tonKho == null) return NotFound();

            if (tonKho.SoLuongTon != 0 || await _context.ChiTietDonHangB2Bs.AnyAsync(c => c.MaSanPham == tonKho.MaSanPham && c.DonHang != null &&
                (c.DonHang.MaNPPBan == maDoanhNghiep || c.DonHang.MaNPPMua == maDoanhNghiep)))
                return BadRequest("Sản phẩm còn tồn kho hoặc đã có giao dịch. Hãy ngừng chào bán thay vì xóa.");
            _context.TonKhoNPPs.Remove(tonKho);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi kho.";
            return RedirectToAction("Index");
        }
    }
}
