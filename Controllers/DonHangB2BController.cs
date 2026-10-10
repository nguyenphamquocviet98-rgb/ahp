using code.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;
using System.Security.Claims;

namespace code.Controllers
{
    [Authorize]
    public class DonHangB2BController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DonHangB2BController(ApplicationDbContext context)
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
        /// Danh sách đơn hàng tôi đã MUA từ các NPP khác
        /// </summary>
        public async Task<IActionResult> DonMua(string? status)
        {
            int dnId = GetCurrentMaDoanhNghiep();
            var query = _context.DonHangB2Bs
                .Include(d => d.NPPBan)
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(c => c.SanPham)
                .Where(d => User.IsPlatformAdmin() || (dnId > 0 && d.MaNPPMua == dnId)); // Nếu là admin sàn thì xem được hết

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(d => d.TrangThai == status);
            }

            var list = await query.OrderByDescending(d => d.NgayDatHang).ToListAsync();

            ViewBag.Status = status;
            ViewBag.CurrentDNId = dnId;
            return View("DonMua", list);
        }

        public async Task<IActionResult> DonDaDat(string? status) => await DonMua(status);

        /// <summary>
        /// Danh sách đơn hàng các đối tác đặt MUA TỪ TÔI (Tôi là bên Bán)
        /// </summary>
        public async Task<IActionResult> DonBan(string? status)
        {
            int dnId = GetCurrentMaDoanhNghiep();
            var query = _context.DonHangB2Bs
                .Include(d => d.NPPMua)
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(c => c.SanPham)
                .Where(d => User.IsPlatformAdmin() || (dnId > 0 && d.MaNPPBan == dnId));

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(d => d.TrangThai == status);
            }

            var list = await query.OrderByDescending(d => d.NgayDatHang).ToListAsync();

            ViewBag.Status = status;
            ViewBag.CurrentDNId = dnId;
            return View("DonBan", list);
        }

        public async Task<IActionResult> DonNhanDuoc(string? status) => await DonBan(status);

        /// <summary>
        /// Chi tiết đơn hàng B2B
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var don = await _context.DonHangB2Bs
                .Include(d => d.NPPMua)
                .Include(d => d.NPPBan)
                .Include(d => d.NguoiDat)
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(c => c.SanPham)
                        .ThenInclude(s => s!.DonViTinh)
                .FirstOrDefaultAsync(d => d.MaDonHang == id);

            if (don == null) return NotFound();
            if (!User.CanViewOrder(don)) return Forbid();

            ViewBag.CurrentDNId = GetCurrentMaDoanhNghiep();
            ViewBag.DanhGia = await _context.DanhGiaNPPs.FirstOrDefaultAsync(d => d.MaDonHang == id);
            return View(don);
        }

        /// <summary>
        /// Bên Bán: Duyệt đơn hàng sỉ (Trừ kho bên Bán)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DuyetDon(int id)
        {
            var don = await _context.DonHangB2Bs
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaDonHang == id);

            if (don != null && (!User.IsCompanyManager() || don.MaNPPBan != User.CompanyId())) return Forbid();

            if (don == null || don.TrangThai != "cho_duyet")
            {
                TempData["ErrorMessage"] = "Đơn hàng không hợp lệ hoặc đã được xử lý!";
                return RedirectToAction("Details", new { id });
            }

            if (!don.ChiTietDonHangs.Any() || don.ChiTietDonHangs.Any(c => c.SoLuong <= 0))
                return BadRequest("Đơn hàng chưa có chi tiết hợp lệ.");
            // Trừ số lượng tồn trong TonKhoNPP của bên bán
            foreach (var ct in don.ChiTietDonHangs)
            {
                var khoBan = await _context.TonKhoNPPs
                    .FirstOrDefaultAsync(t => t.MaDoanhNghiep == don.MaNPPBan && t.MaSanPham == ct.MaSanPham);

                if (khoBan == null) return BadRequest("Sản phẩm không còn trong kho bên bán.");
                if (khoBan != null)
                {
                    if (khoBan.SoLuongTon < ct.SoLuong)
                    {
                        TempData["ErrorMessage"] = $"Kho không đủ hàng để duyệt đơn! Sản phẩm mã #{ct.MaSanPham} chỉ còn {khoBan.SoLuongTon}.";
                        return RedirectToAction("Details", new { id });
                    }
                    khoBan.SoLuongTon -= ct.SoLuong;
                    khoBan.NgayCapNhat = DateTime.Now;
                }
            }

            don.TrangThai = "da_duyet";
            don.NgayCapNhat = DateTime.Now;
            OrderAudit.Record(don, User);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã duyệt đơn hàng [{don.MaDonHangCode}] thành công! Kho đã được cập nhật.";
            return RedirectToAction("Details", new { id });
        }

        /// <summary>
        /// Bên Bán: Bắt đầu giao hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GiaoHang(int id)
        {
            var don = await _context.DonHangB2Bs.FindAsync(id);
            if (don != null && (!User.IsCompanyMember() || don.MaNPPBan != User.CompanyId())) return Forbid();

            if (don == null || don.TrangThai != "da_duyet")
            {
                TempData["ErrorMessage"] = "Chỉ có thể giao hàng cho đơn đã duyệt!";
                return RedirectToAction("Details", new { id });
            }

            don.TrangThai = "dang_giao";
            don.NgayCapNhat = DateTime.Now;
            OrderAudit.Record(don, User);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đơn hàng [{don.MaDonHangCode}] đã được chuyển sang trạng thái đang giao hàng.";
            return RedirectToAction("Details", new { id });
        }

        /// <summary>
        /// Bên Mua: Xác nhận đã nhận hàng (Tự động cộng vào TonKhoNPP của bên Mua)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanNhanHang(int id)
        {
            var don = await _context.DonHangB2Bs
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaDonHang == id);

            if (don != null && (!User.IsCompanyMember() || don.MaNPPMua != User.CompanyId())) return Forbid();

            if (don == null || don.TrangThai != "dang_giao")
            {
                TempData["ErrorMessage"] = "Chỉ có thể xác nhận nhận hàng khi đơn đang được giao!";
                return RedirectToAction("Details", new { id });
            }

            // Tự động cộng số lượng vào TonKhoNPP của bên mua
            foreach (var ct in don.ChiTietDonHangs)
            {
                var khoMua = await _context.TonKhoNPPs
                    .FirstOrDefaultAsync(t => t.MaDoanhNghiep == don.MaNPPMua && t.MaSanPham == ct.MaSanPham);

                if (khoMua != null)
                {
                    khoMua.SoLuongTon = checked(khoMua.SoLuongTon + ct.SoLuong);
                    khoMua.NgayCapNhat = DateTime.Now;
                }
                else
                {
                    // Nếu chưa có trong kho thì thêm mới
                    _context.TonKhoNPPs.Add(new TonKhoNPP
                    {
                        MaDoanhNghiep = don.MaNPPMua,
                        MaSanPham = ct.MaSanPham,
                        SoLuongTon = ct.SoLuong,
                        GiaSi = ct.DonGia,
                        ThoiGianGiaoHang = 1,
                        DangChaoBan = false, // Mới nhập về tạm thời chưa đăng bán lại
                        NgayCapNhat = DateTime.Now
                    });
                }
            }

            don.TrangThai = "hoan_thanh";
            don.NgayCapNhat = DateTime.Now;
            OrderAudit.Record(don, User);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xác nhận nhận hàng thành công! Hàng hóa đã được tự động cộng vào kho của bạn.";
            return RedirectToAction("Details", new { id });
        }

        /// <summary>
        /// Hủy đơn hàng (Hoàn lại kho bên Bán nếu đơn đã duyệt hoặc đang giao)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyDon(int id, string? lyDo)
        {
            var don = await _context.DonHangB2Bs
                .Include(d => d.ChiTietDonHangs)
                .FirstOrDefaultAsync(d => d.MaDonHang == id);

            if (don != null && (!User.IsCompanyManager() || (don.MaNPPMua != User.CompanyId() && don.MaNPPBan != User.CompanyId()))) return Forbid();

            if (don == null || don.TrangThai == "hoan_thanh" || don.TrangThai == "huy")
            {
                TempData["ErrorMessage"] = "Đơn hàng không thể hủy!";
                return RedirectToAction("Details", new { id });
            }

            if (don.TrangThai == "dang_giao")
                return BadRequest("Hàng đã giao vận chuyển. Cần đối soát hàng trả về trước khi hoàn kho; không thể hủy trực tiếp.");
            // Hoàn kho khi hủy trước lúc giao hàng
            if (don.TrangThai == "da_duyet" || don.TrangThai == "dang_giao")
            {
                foreach (var ct in don.ChiTietDonHangs)
                {
                    var khoBan = await _context.TonKhoNPPs
                        .FirstOrDefaultAsync(t => t.MaDoanhNghiep == don.MaNPPBan && t.MaSanPham == ct.MaSanPham);
                    if (khoBan == null) return BadRequest("Không tìm thấy kho để hoàn hàng.");
                    if (khoBan != null)
                    {
                        khoBan.SoLuongTon = checked(khoBan.SoLuongTon + ct.SoLuong);
                        khoBan.NgayCapNhat = DateTime.Now;
                    }
                }
            }

            don.TrangThai = "huy";
            var note = (don.GhiChu != null ? don.GhiChu + " | " : "") + "Lý do hủy: " + (lyDo ?? "Người dùng hủy");
            if (note.Length > 500) return BadRequest("Ghi chú và lý do hủy tối đa 500 ký tự.");
            don.GhiChu = note;
            don.NgayCapNhat = DateTime.Now;
            OrderAudit.Record(don, User);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã hủy đơn hàng [{don.MaDonHangCode}].";
            return RedirectToAction("Details", new { id });
        }

        /// <summary>
        /// Bên Mua: Gửi đánh giá cho bên Bán sau khi đơn hoàn thành
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuiDanhGia(int maDonHang, int soSao, string? noiDung)
        {
            var don = await _context.DonHangB2Bs.FindAsync(maDonHang);
            if (don == null || don.TrangThai != "hoan_thanh")
            {
                TempData["ErrorMessage"] = "Chỉ có thể đánh giá đơn hàng đã hoàn thành!";
                return RedirectToAction("Details", new { id = maDonHang });
            }

            int currentDNId = GetCurrentMaDoanhNghiep();
            if (!User.IsCompanyMember() || don.MaNPPMua != currentDNId)
            {
                TempData["ErrorMessage"] = "Chỉ bên mua mới có quyền gửi đánh giá cho đối tác!";
                return RedirectToAction("Details", new { id = maDonHang });
            }

            var daDanhGia = await _context.DanhGiaNPPs.AnyAsync(d => d.MaDonHang == maDonHang);
            if (daDanhGia)
            {
                TempData["ErrorMessage"] = "Đơn hàng này đã được đánh giá trước đó!";
                return RedirectToAction("Details", new { id = maDonHang });
            }

            var danhGia = new DanhGiaNPP
            {
                MaDonHang = maDonHang,
                MaNPPMua = don.MaNPPMua,
                MaNPPBan = don.MaNPPBan,
                SoSao = Math.Clamp(soSao, 1, 5),
                NoiDung = noiDung,
                NgayDanhGia = DateTime.Now
            };

            _context.DanhGiaNPPs.Add(danhGia);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Gửi đánh giá thành công! Điểm uy tín của đối tác đã được tự động cập nhật lại.";
            return RedirectToAction("Details", new { id = maDonHang });
        }
    }
}
