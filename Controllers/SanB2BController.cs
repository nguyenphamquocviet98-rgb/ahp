using code.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;
using System.Security.Claims;

namespace code.Controllers
{
    public class SanB2BController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SanB2BController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Sàn Giao dịch Chợ Sỉ B2B: Xem tất cả sản phẩm đang chào bán từ các NPP
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> Index(string? search, int? maDanhMuc, string? loaiNPP, string? sort, int page = 1)
        {
            var query = _context.TonKhoNPPs
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DanhMuc)
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DonViTinh)
                .Include(t => t.DoanhNghiep)
                .Where(t => t.DangChaoBan && t.SoLuongTon > 0 && t.DoanhNghiep != null && t.DoanhNghiep.TrangThai);

            // Tìm kiếm theo tên sản phẩm hoặc tên NPP
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t =>
                    (t.SanPham != null && t.SanPham.TenSanPham != null && t.SanPham.TenSanPham.Contains(search)) ||
                    (t.SanPham != null && t.SanPham.MaSanPhamCode != null && t.SanPham.MaSanPhamCode.Contains(search)) ||
                    (t.DoanhNghiep != null && t.DoanhNghiep.TenDoanhNghiep.Contains(search)));
            }

            // Lọc theo Danh mục
            if (maDanhMuc.HasValue && maDanhMuc.Value > 0)
            {
                query = query.Where(t => t.SanPham != null && t.SanPham.MaDanhMuc == maDanhMuc.Value);
            }

            // Lọc theo loại NPP: Mall hay tất cả
            if (loaiNPP == "mall")
            {
                query = query.Where(t => t.DoanhNghiep != null && t.DoanhNghiep.LaGianHangMall);
            }

            // Sắp xếp
            query = sort switch
            {
                "gia_tang" => query.OrderBy(t => t.GiaSi),
                "gia_giam" => query.OrderByDescending(t => t.GiaSi),
                "giao_nhanh" => query.OrderBy(t => t.ThoiGianGiaoHang),
                "uy_tin" => query.OrderByDescending(t => t.DoanhNghiep != null ? t.DoanhNghiep.DiemUyTin : 0),
                _ => query.OrderByDescending(t => t.DoanhNghiep != null && t.DoanhNghiep.LaGianHangMall).ThenByDescending(t => t.NgayCapNhat)
            };

            int totalItems = await query.CountAsync();
            int pageSize = 12;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var list = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;

            // Thống kê nhanh trên banner sàn
            ViewBag.TongSanPham = totalItems;
            ViewBag.TongNPP = await _context.DoanhNghieps.CountAsync(d => d.TrangThai);
            ViewBag.TongMall = await _context.DoanhNghieps.CountAsync(d => d.LaGianHangMall && d.TrangThai);

            // Truyền danh sách danh mục để lọc
            ViewBag.DanhMucs = await _context.DanhMucs.ToListAsync();
            ViewBag.Search = search;
            ViewBag.MaDanhMuc = maDanhMuc;
            ViewBag.LoaiNPP = loaiNPP;
            ViewBag.Sort = sort;

            // Thông tin doanh nghiệp hiện tại của người dùng
            var maDNClaim = User.FindFirstValue("MaDoanhNghiep");
            int.TryParse(maDNClaim, out int currentDNId);
            ViewBag.CurrentMaDoanhNghiep = currentDNId;

            // Lấy danh sách banner quảng cáo trang nhất đang hoạt động
            ViewBag.Banners = await _context.QuangCaoBanners
                .Include(b => b.DoanhNghiep)
                .Where(b => b.TrangThai == "hoat_dong" && b.NgayKetThuc >= DateTime.Now)
                .OrderByDescending(b => b.ChiPhi)
                .ToListAsync();

            return View(list);
        }

        /// <summary>
        /// Xem chi tiết sản phẩm chào bán B2B
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> ChiTiet(int id)
        {
            // 1. Tìm theo MaTonKhoNPP
            var tonKho = await _context.TonKhoNPPs
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DanhMuc)
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DonViTinh)
                .Include(t => t.DoanhNghiep)
                .FirstOrDefaultAsync(t => t.MaTonKhoNPP == id);

            if (tonKho == null || !tonKho.DangChaoBan || tonKho.DoanhNghiep?.TrangThai != true)
                return NotFound();

            // Lấy thêm các sản phẩm cùng ngành hàng
            var cungDanhMuc = await _context.TonKhoNPPs
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DanhMuc)
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DonViTinh)
                .Include(t => t.DoanhNghiep)
                .Where(t => t.DangChaoBan && t.MaTonKhoNPP != tonKho.MaTonKhoNPP &&
                            tonKho.SanPham != null && t.SanPham != null &&
                            t.SanPham.MaDanhMuc == tonKho.SanPham.MaDanhMuc)
                .Take(4)
                .ToListAsync();

            ViewBag.CungDanhMuc = cungDanhMuc;

            var maDNClaim = User.FindFirstValue("MaDoanhNghiep");
            int.TryParse(maDNClaim, out int currentDNId);
            ViewBag.CurrentMaDoanhNghiep = currentDNId;

            return View(tonKho);
        }

        /// <summary>
        /// Tạo đơn hàng sỉ B2B nhanh từ mặt hàng đang chào bán
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatHangNhanh(int maTonKhoNPP, int soLuong, string? ghiChu, decimal? giaDuKien = null)
        {
            if (!User.IsCompanyMember()) return Forbid();
            var maDNClaim = User.FindFirstValue("MaDoanhNghiep");
            if (!int.TryParse(maDNClaim, out int currentDNId) || currentDNId <= 0)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập bằng tài khoản thuộc Doanh nghiệp NPP để đặt hàng sỉ!";
                return RedirectToAction("Index");
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out int userId);

            var tonKho = await _context.TonKhoNPPs
                .Include(t => t.SanPham)
                .Include(t => t.DoanhNghiep)
                .FirstOrDefaultAsync(t => t.MaTonKhoNPP == maTonKhoNPP);

            if (tonKho == null || !tonKho.DangChaoBan || tonKho.DoanhNghiep?.TrangThai != true || tonKho.SanPham?.TrangThai != true || tonKho.GiaSi <= 0)
            {
                TempData["ErrorMessage"] = "Mặt hàng chào sỉ không tồn tại hoặc đã ngừng chào bán!";
                return RedirectToAction("Index");
            }

            if (tonKho.MaDoanhNghiep == currentDNId)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự đặt hàng từ chính kho của doanh nghiệp mình!";
                return RedirectToAction("Index");
            }

            if (giaDuKien.HasValue && giaDuKien.Value != tonKho.GiaSi)
            {
                TempData["ErrorMessage"] = "Giá chào hàng đã thay đổi. Hãy kiểm tra giá hiện tại và xác nhận đặt hàng lại.";
                return RedirectToAction("ChiTiet", new { id = maTonKhoNPP });
            }
            if (soLuong <= 0 || soLuong > tonKho.SoLuongTon)
            {
                TempData["ErrorMessage"] = $"Số lượng đặt ({soLuong}) không hợp lệ! Hiện kho chỉ còn sẵn {tonKho.SoLuongTon}.";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid || userId <= 0 || ghiChu?.Length > 500) return BadRequest("Thông tin đặt hàng không hợp lệ.");
            // Sinh mã đơn hàng B2B độc nhất
            var maDonCode = "B2B-" + Guid.NewGuid().ToString("N")[..16];

            var donHang = new DonHangB2B
            {
                MaDonHangCode = maDonCode,
                MaNPPMua = currentDNId,
                MaNPPBan = tonKho.MaDoanhNghiep,
                MaNguoiDat = userId,
                NgayDatHang = DateTime.Now,
                TrangThai = "cho_duyet",
                GhiChu = ghiChu,
                TongTien = soLuong * tonKho.GiaSi,
                NgayCapNhat = DateTime.Now
            };



            // Thêm chi tiết đơn
            var chiTiet = new ChiTietDonHangB2B
            {

                MaSanPham = tonKho.MaSanPham,
                SoLuong = soLuong,
                DonGia = tonKho.GiaSi
            };

            donHang.ChiTietDonHangs.Add(chiTiet);
            OrderAudit.Record(donHang, User);
            _context.DonHangB2Bs.Add(donHang);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đặt đơn hàng sỉ [{maDonCode}] thành công! Đã gửi thông báo đến đối tác {tonKho.DoanhNghiep?.TenDoanhNghiep}.";
            return RedirectToAction("DonMua", "DonHangB2B");
        }
    }
}
