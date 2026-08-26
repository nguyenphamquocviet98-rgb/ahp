using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;

namespace code.Controllers
{
    [Authorize]
    public class PhieuNhapController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PhieuNhapController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PhieuNhap
        public async Task<IActionResult> Index(string? search, string? trangThai)
        {
            var query = _context.PhieuNhaps
                .Include(p => p.NhaCungCap)
                .Include(p => p.NguoiDung)
                .Include(p => p.ChiTietPhieuNhaps)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(p => (p.MaPhieuNhapCode != null && p.MaPhieuNhapCode.Contains(s)) ||
                                         (p.NhaCungCap != null && p.NhaCungCap.TenNhaCungCap != null && p.NhaCungCap.TenNhaCungCap.Contains(s)));
                ViewData["Search"] = s;
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
            {
                query = query.Where(p => p.TrangThai == trangThai);
                ViewData["SelectedTrangThai"] = trangThai;
            }

            var list = await query.OrderByDescending(p => p.NgayNhap ?? p.NgayTao).ToListAsync();
            return View(list);
        }

        // GET: PhieuNhap/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phieuNhap = await _context.PhieuNhaps
                .Include(p => p.NhaCungCap)
                .Include(p => p.NguoiDung)
                .Include(p => p.ChiTietPhieuNhaps)
                    .ThenInclude(c => c.SanPham)
                        .ThenInclude(s => s!.DonViTinh)
                .FirstOrDefaultAsync(m => m.MaPhieuNhap == id);

            if (phieuNhap == null)
            {
                return NotFound();
            }

            // Danh sách sản phẩm để thêm vào chi tiết phiếu nhập
            ViewBag.SanPhams = await _context.SanPhams
                .Where(s => s.TrangThai == true)
                .Include(s => s.DonViTinh)
                .OrderBy(s => s.TenSanPham)
                .ToListAsync();

            return View(phieuNhap);
        }

        // GET: PhieuNhap/Create
        public async Task<IActionResult> Create(int? maNhaCungCap)
        {
            // Lấy nhà cung cấp được AHP đề xuất gần nhất
            var latestDeXuat = await _context.KetQuaXepHangs
                .Include(k => k.NhaCungCap)
                .Include(k => k.KyDanhGia)
                .Where(k => k.DeXuat == true)
                .OrderByDescending(k => k.MaKyDanhGia)
                .FirstOrDefaultAsync();

            ViewBag.NhaCungCapAHPDeXuat = latestDeXuat;

            int? selectedNCC = maNhaCungCap ?? latestDeXuat?.MaNhaCungCap;
            await PopulateDropdownsAsync(selectedNCC);

            var model = new PhieuNhap
            {
                MaNhaCungCap = selectedNCC,
                NgayNhap = DateTime.Today,
                TrangThai = "nhap"
            };
            return View(model);
        }

        // POST: PhieuNhap/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhieuNhap model)
        {
            if (model.MaNhaCungCap == null || model.MaNhaCungCap <= 0)
            {
                ModelState.AddModelError("MaNhaCungCap", "Vui lòng chọn nhà cung cấp.");
            }

            if (ModelState.IsValid)
            {
                // Lấy ID người dùng hiện tại
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("MaNguoiDung")?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    model.MaNguoiDung = userId;
                }

                model.NgayTao = DateTime.Now;
                model.TrangThai = string.IsNullOrEmpty(model.TrangThai) ? "nhap" : model.TrangThai;
                model.TongTien = 0;

                _context.PhieuNhaps.Add(model);
                await _context.SaveChangesAsync();

                // Tự động sinh mã phiếu nếu chưa có (ví dụ PN0001)
                if (string.IsNullOrWhiteSpace(model.MaPhieuNhapCode))
                {
                    model.MaPhieuNhapCode = $"PN{model.MaPhieuNhap:D4}";
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"Tạo phiếu nhập '{model.MaPhieuNhapCode}' thành công! Hãy thêm các sản phẩm vào phiếu.";
                return RedirectToAction(nameof(Details), new { id = model.MaPhieuNhap });
            }

            await PopulateDropdownsAsync(model.MaNhaCungCap);
            return View(model);
        }

        // GET: PhieuNhap/Edit/5
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phieuNhap = await _context.PhieuNhaps.FindAsync(id);
            if (phieuNhap == null)
            {
                return NotFound();
            }

            await PopulateDropdownsAsync(phieuNhap.MaNhaCungCap);
            return View(phieuNhap);
        }

        // POST: PhieuNhap/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Edit(int id, PhieuNhap model)
        {
            if (id != model.MaPhieuNhap)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.PhieuNhaps.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    existing.MaPhieuNhapCode = model.MaPhieuNhapCode?.Trim().ToUpper();
                    existing.MaNhaCungCap = model.MaNhaCungCap;
                    existing.NgayNhap = model.NgayNhap;
                    existing.TrangThai = model.TrangThai;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thông tin phiếu nhập thành công!";
                    return RedirectToAction(nameof(Details), new { id = model.MaPhieuNhap });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.PhieuNhaps.Any(e => e.MaPhieuNhap == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            await PopulateDropdownsAsync(model.MaNhaCungCap);
            return View(model);
        }

        // POST: PhieuNhap/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> Delete(int id)
        {
            var phieuNhap = await _context.PhieuNhaps
                .Include(p => p.ChiTietPhieuNhaps)
                .FirstOrDefaultAsync(p => p.MaPhieuNhap == id);

            if (phieuNhap == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiếu nhập cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            // Xóa các chi tiết trước (Trigger sẽ tự động trừ lại tồn kho nếu có)
            if (phieuNhap.ChiTietPhieuNhaps.Any())
            {
                _context.ChiTietPhieuNhaps.RemoveRange(phieuNhap.ChiTietPhieuNhaps);
            }

            _context.PhieuNhaps.Remove(phieuNhap);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa phiếu nhập '{phieuNhap.MaPhieuNhapCode ?? phieuNhap.MaPhieuNhap.ToString()}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: PhieuNhap/ThemChiTiet
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemChiTiet(int maPhieuNhap, int maSanPham, int soLuong, decimal donGia)
        {
            if (soLuong <= 0)
            {
                TempData["ErrorMessage"] = "Số lượng nhập phải lớn hơn 0.";
                return RedirectToAction(nameof(Details), new { id = maPhieuNhap });
            }

            if (donGia < 0)
            {
                TempData["ErrorMessage"] = "Đơn giá không được âm.";
                return RedirectToAction(nameof(Details), new { id = maPhieuNhap });
            }

            var phieuNhap = await _context.PhieuNhaps.FindAsync(maPhieuNhap);
            if (phieuNhap == null)
            {
                TempData["ErrorMessage"] = "Phiếu nhập không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra xem sản phẩm đã có trong phiếu nhập này chưa
            var existingDetail = await _context.ChiTietPhieuNhaps
                .FirstOrDefaultAsync(c => c.MaPhieuNhap == maPhieuNhap && c.MaSanPham == maSanPham);

            if (existingDetail != null)
            {
                existingDetail.SoLuong += soLuong;
                existingDetail.DonGia = donGia;
            }
            else
            {
                var detail = new ChiTietPhieuNhap
                {
                    MaPhieuNhap = maPhieuNhap,
                    MaSanPham = maSanPham,
                    SoLuong = soLuong,
                    DonGia = donGia
                };
                _context.ChiTietPhieuNhaps.Add(detail);
            }

            // Ghi nhật ký vào LichSuNhapHang
            var lichSu = new LichSuNhapHang
            {
                MaSanPham = maSanPham,
                MaPhieuNhap = maPhieuNhap,
                SoLuongThayDoi = soLuong,
                LoaiThayDoi = "nhap",
                NgayTao = DateTime.Now
            };
            _context.LichSuNhapHangs.Add(lichSu);

            // Cập nhật tồn kho nếu phiếu đã ở trạng thái xác nhận hoặc được cộng trực tiếp
            var tonKho = await _context.TonKhos.FirstOrDefaultAsync(t => t.MaSanPham == maSanPham);
            if (tonKho != null)
            {
                tonKho.SoLuongTon = (tonKho.SoLuongTon ?? 0) + soLuong;
                tonKho.NgayCapNhat = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            // Cập nhật lại tổng tiền phiếu nhập
            var tongTienMoi = await _context.ChiTietPhieuNhaps
                .Where(c => c.MaPhieuNhap == maPhieuNhap)
                .SumAsync(c => c.SoLuong * c.DonGia ?? 0);
            phieuNhap.TongTien = tongTienMoi;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã thêm sản phẩm vào phiếu nhập và cập nhật kho thành công!";
            return RedirectToAction(nameof(Details), new { id = maPhieuNhap });
        }

        // POST: PhieuNhap/SuaChiTiet
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> SuaChiTiet(int maChiTiet, int soLuong, decimal donGia)
        {
            if (soLuong <= 0)
            {
                TempData["ErrorMessage"] = "Số lượng nhập phải lớn hơn 0.";
                return RedirectToAction(nameof(Index));
            }

            var detail = await _context.ChiTietPhieuNhaps.FindAsync(maChiTiet);
            if (detail == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dòng chi tiết phiếu nhập.";
                return RedirectToAction(nameof(Index));
            }

            int soLuongCu = detail.SoLuong ?? 0;
            int chenhLech = soLuong - soLuongCu;

            detail.SoLuong = soLuong;
            detail.DonGia = donGia;

            // Ghi nhận biến động điều chỉnh kho
            if (chenhLech != 0 && detail.MaSanPham.HasValue)
            {
                var lichSu = new LichSuNhapHang
                {
                    MaSanPham = detail.MaSanPham.Value,
                    MaPhieuNhap = detail.MaPhieuNhap,
                    SoLuongThayDoi = chenhLech,
                    LoaiThayDoi = "dieuchinh",
                    NgayTao = DateTime.Now
                };
                _context.LichSuNhapHangs.Add(lichSu);

                var tonKho = await _context.TonKhos.FirstOrDefaultAsync(t => t.MaSanPham == detail.MaSanPham.Value);
                if (tonKho != null)
                {
                    tonKho.SoLuongTon = (tonKho.SoLuongTon ?? 0) + chenhLech;
                    tonKho.NgayCapNhat = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();

            // Cập nhật lại tổng tiền phiếu nhập
            if (detail.MaPhieuNhap.HasValue)
            {
                var phieu = await _context.PhieuNhaps.FindAsync(detail.MaPhieuNhap.Value);
                if (phieu != null)
                {
                    var tong = await _context.ChiTietPhieuNhaps
                        .Where(c => c.MaPhieuNhap == detail.MaPhieuNhap.Value)
                        .SumAsync(c => c.SoLuong * c.DonGia ?? 0);
                    phieu.TongTien = tong;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["SuccessMessage"] = "Cập nhật sản phẩm trong phiếu nhập thành công!";
            return RedirectToAction(nameof(Details), new { id = detail.MaPhieuNhap });
        }

        // POST: PhieuNhap/XoaChiTiet
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> XoaChiTiet(int maChiTiet)
        {
            var detail = await _context.ChiTietPhieuNhaps.FindAsync(maChiTiet);
            if (detail == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dòng chi tiết cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            int? maPhieu = detail.MaPhieuNhap;
            int? maSp = detail.MaSanPham;
            int soLuongXoa = detail.SoLuong ?? 0;

            _context.ChiTietPhieuNhaps.Remove(detail);

            // Giảm tồn kho và ghi nhật ký
            if (maSp.HasValue && soLuongXoa > 0)
            {
                var lichSu = new LichSuNhapHang
                {
                    MaSanPham = maSp.Value,
                    MaPhieuNhap = maPhieu,
                    SoLuongThayDoi = -soLuongXoa,
                    LoaiThayDoi = "dieuchinh",
                    NgayTao = DateTime.Now
                };
                _context.LichSuNhapHangs.Add(lichSu);

                var tonKho = await _context.TonKhos.FirstOrDefaultAsync(t => t.MaSanPham == maSp.Value);
                if (tonKho != null)
                {
                    tonKho.SoLuongTon = Math.Max(0, (tonKho.SoLuongTon ?? 0) - soLuongXoa);
                    tonKho.NgayCapNhat = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();

            // Cập nhật lại tổng tiền phiếu
            if (maPhieu.HasValue)
            {
                var phieu = await _context.PhieuNhaps.FindAsync(maPhieu.Value);
                if (phieu != null)
                {
                    var tong = await _context.ChiTietPhieuNhaps
                        .Where(c => c.MaPhieuNhap == maPhieu.Value)
                        .SumAsync(c => c.SoLuong * c.DonGia ?? 0);
                    phieu.TongTien = tong;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi phiếu nhập và cập nhật tồn kho!";
            return RedirectToAction(nameof(Details), new { id = maPhieu });
        }

        // POST: PhieuNhap/DoiTrangThai
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> DoiTrangThai(int maPhieuNhap, string trangThaiMoi)
        {
            var phieuNhap = await _context.PhieuNhaps.FindAsync(maPhieuNhap);
            if (phieuNhap == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiếu nhập.";
                return RedirectToAction(nameof(Index));
            }

            phieuNhap.TrangThai = trangThaiMoi;
            await _context.SaveChangesAsync();

            var trangThaiText = trangThaiMoi switch
            {
                "xacnhan" => "Đã xác nhận",
                "huy" => "Đã hủy",
                _ => "Chờ xử lý"
            };

            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái phiếu '{phieuNhap.MaPhieuNhapCode}' thành '{trangThaiText}'!";
            return RedirectToAction(nameof(Details), new { id = maPhieuNhap });
        }

        private async Task PopulateDropdownsAsync(int? selectedNhaCungCap = null)
        {
            var deXuatNccId = await _context.KetQuaXepHangs
                .Where(k => k.DeXuat == true)
                .OrderByDescending(k => k.MaKyDanhGia)
                .Select(k => k.MaNhaCungCap)
                .FirstOrDefaultAsync();

            var nccList = await _context.NhaCungCaps
                .Where(n => n.TrangThai == true)
                .OrderBy(n => n.TenNhaCungCap)
                .Select(n => new
                {
                    n.MaNhaCungCap,
                    TenHienThi = (deXuatNccId.HasValue && n.MaNhaCungCap == deXuatNccId.Value)
                        ? $"⭐ {n.TenNhaCungCap} (AHP Đề xuất)"
                        : n.TenNhaCungCap
                })
                .ToListAsync();

            ViewBag.NhaCungCaps = new SelectList(nccList, "MaNhaCungCap", "TenHienThi", selectedNhaCungCap);
        }
    }
}
