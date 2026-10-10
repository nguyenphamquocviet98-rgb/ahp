using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;

namespace code.Controllers
{
    [Authorize(Roles = "admin,admin_san")]
    public class DashboardAdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Dashboard Tài Chính & Doanh Thu Sàn B2B dành cho Admin Sàn
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // 1. Thống kê nguồn thu nhập của Admin sàn
            var danhSachThu = await _context.DoanhThuSans
                .Include(d => d.DoanhNghiep)
                .OrderByDescending(d => d.NgayGiaoDich)
                .ToListAsync();

            decimal tongDoanhThu = danhSachThu.Where(d => d.TrangThai == "da_thu").Sum(d => d.SoTien);
            decimal doanhThuMall = danhSachThu.Where(d => d.TrangThai == "da_thu" && d.LoaiThu == "phi_mall").Sum(d => d.SoTien);
            decimal doanhThuQuangCao = danhSachThu.Where(d => d.TrangThai == "da_thu" && d.LoaiThu == "phi_quang_cao").Sum(d => d.SoTien);

            // 2. Thống kê quy mô giao dịch hàng hóa qua sàn (GMV)
            var donHangHoanThanh = await _context.DonHangB2Bs
                .Where(d => d.TrangThai == "hoan_thanh")
                .ToListAsync();

            decimal tongGMV = donHangHoanThanh.Sum(d => d.TongTien);
            int tongDonHoanThanh = donHangHoanThanh.Count;

            // 3. Quy mô doanh nghiệp trên sàn
            int tongNPP = await _context.DoanhNghieps.CountAsync(d => d.TrangThai);
            int tongMall = await _context.DoanhNghieps.CountAsync(d => d.LaGianHangMall && d.TrangThai);
            int tongBannerHoatDong = await _context.QuangCaoBanners.CountAsync(b => b.TrangThai == "hoat_dong");

            // Truyền dữ liệu sang View
            ViewBag.TongDoanhThu = tongDoanhThu;
            ViewBag.DoanhThuMall = doanhThuMall;
            ViewBag.DoanhThuQuangCao = doanhThuQuangCao;
            ViewBag.TongGMV = tongGMV;
            ViewBag.TongDonHoanThanh = tongDonHoanThanh;
            ViewBag.TongNPP = tongNPP;
            ViewBag.TongMall = tongMall;
            ViewBag.TongBannerHoatDong = tongBannerHoatDong;

            return View(danhSachThu);
        }
    }
}
