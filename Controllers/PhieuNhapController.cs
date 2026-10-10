using code.Services;
using System.IO;
using System.Security.Claims;
using ClosedXML.Excel;
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

            var maxId = await _context.PhieuNhaps.MaxAsync(p => (int?)p.MaPhieuNhap) ?? 0;
            var model = new PhieuNhap
            {
                MaPhieuNhapCode = "Sinh khi lưu",
                MaNhaCungCap = selectedNCC,
                NgayNhap = DateTime.Today,
                TrangThai = "nhap"
            };
            return View(model);
        }

        // POST: PhieuNhap/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaNhaCungCap,NgayNhap")] PhieuNhap model)
        {
            if (!User.IsCompanyMember()) return Forbid();
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
                model.TrangThai = "nhap";
                model.MaPhieuNhap = 0;
                model.ChiTietPhieuNhaps.Clear();
                model.LichSuNhapHangs.Clear();
                model.NguoiDung = null;
                model.NhaCungCap = null;
                model.MaPhieuNhapCode = "PN" + Guid.NewGuid().ToString("N")[..18];
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
        [Authorize(Roles = "quanly,chu_npp")]
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
        [Authorize(Roles = "quanly,chu_npp")]
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

                    if ((existing.PhienBanNhapKho < 2 || existing.TrangThai != "nhap")) return BadRequest("Phiếu đã xử lý không được sửa.");
                    existing.MaPhieuNhapCode = model.MaPhieuNhapCode?.Trim().ToUpper();
                    existing.MaNhaCungCap = model.MaNhaCungCap;
                    existing.NgayNhap = model.NgayNhap;


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
        [Authorize(Roles = "quanly,chu_npp")]
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

            if ((phieuNhap.PhienBanNhapKho < 2 || phieuNhap.TrangThai != "nhap")) return BadRequest("Chỉ được xóa phiếu nháp.");
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

            if (!User.IsCompanyMember()) return Forbid();
            if ((phieuNhap.PhienBanNhapKho < 2 || phieuNhap.TrangThai != "nhap")) return BadRequest("Chỉ được sửa chi tiết phiếu nháp.");
            if (!await _context.SanPhams.AnyAsync(p => p.MaSanPham == maSanPham && p.TrangThai == true)) return BadRequest("Sản phẩm không hợp lệ.");
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

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã thêm sản phẩm vào phiếu nháp. Kho sẽ tăng khi xác nhận nhận hàng.";
            return RedirectToAction(nameof(Details), new { id = maPhieuNhap });
        }

        // POST: PhieuNhap/SuaChiTiet
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "quanly,chu_npp")]
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

            if (donGia < 0) return BadRequest("Đơn giá không được âm.");
            var parent = await _context.PhieuNhaps.FindAsync(detail.MaPhieuNhap);
            if ((parent == null || parent.PhienBanNhapKho < 2 || parent.TrangThai != "nhap")) return BadRequest("Chỉ được sửa phiếu nháp.");
            int soLuongCu = detail.SoLuong ?? 0;
            int chenhLech = soLuong - soLuongCu;

            detail.SoLuong = soLuong;
            detail.DonGia = donGia;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật sản phẩm trong phiếu nhập thành công!";
            return RedirectToAction(nameof(Details), new { id = detail.MaPhieuNhap });
        }

        // POST: PhieuNhap/XoaChiTiet
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "quanly,chu_npp")]
        public async Task<IActionResult> XoaChiTiet(int maChiTiet)
        {
            var detail = await _context.ChiTietPhieuNhaps.FindAsync(maChiTiet);
            if (detail == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dòng chi tiết cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            int? maPhieu = detail.MaPhieuNhap;
            var parent = await _context.PhieuNhaps.FindAsync(maPhieu);
            if ((parent == null || parent.PhienBanNhapKho < 2 || parent.TrangThai != "nhap")) return BadRequest("Chỉ được xóa chi tiết phiếu nháp.");
            _context.ChiTietPhieuNhaps.Remove(detail);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi phiếu nhập và cập nhật tồn kho!";
            return RedirectToAction(nameof(Details), new { id = maPhieu });
        }

        // POST: PhieuNhap/DoiTrangThai
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "quanly,chu_npp")]
        public async Task<IActionResult> DoiTrangThai(int maPhieuNhap, string trangThaiMoi)
        {
            var phieuNhap = await _context.PhieuNhaps.FindAsync(maPhieuNhap);
            if (phieuNhap == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiếu nhập.";
                return RedirectToAction(nameof(Index));
            }

            if (phieuNhap.PhienBanNhapKho < 2) return BadRequest("Phiếu cũ đã cập nhật kho; cần đối soát trước khi thay đổi.");
            if (trangThaiMoi != "xacnhan" && trangThaiMoi != "huy") return BadRequest("Trạng thái không hợp lệ.");
            if (phieuNhap.TrangThai == trangThaiMoi) return RedirectToAction(nameof(Details), new { id = maPhieuNhap });
            if ((phieuNhap.PhienBanNhapKho < 2 || phieuNhap.TrangThai != "nhap") && !(phieuNhap.TrangThai == "xacnhan" && trangThaiMoi == "huy"))
                return BadRequest("Không thể chuyển trạng thái phiếu đã xử lý.");
            if (trangThaiMoi == "xacnhan" && !await _context.ChiTietPhieuNhaps.AnyAsync(c => c.MaPhieuNhap == maPhieuNhap))
                return BadRequest("Phiếu chưa có hàng để xác nhận.");
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

        // GET: PhieuNhap/ExportExcel
        public async Task<IActionResult> ExportExcel(string? search, string? trangThai)
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
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
            {
                query = query.Where(p => p.TrangThai == trangThai);
            }

            var list = await query.OrderByDescending(p => p.NgayNhap ?? p.NgayTao).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("DanhSachPhieuNhap");

            // Title
            worksheet.Cell("A1").Value = "DANH SÁCH TỔNG HỢP PHIẾU NHẬP HÀNG";
            worksheet.Range("A1:G1").Merge();
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 16;
            worksheet.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#0F172A");
            worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell("A2").Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Người xuất: {User.Identity?.Name ?? "Admin"}";
            worksheet.Range("A2:G2").Merge();
            worksheet.Cell("A2").Style.Font.Italic = true;
            worksheet.Cell("A2").Style.Font.FontSize = 11;
            worksheet.Cell("A2").Style.Font.FontColor = XLColor.FromHtml("#64748B");
            worksheet.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var headers = new string[] { "STT", "Mã phiếu", "Nhà cung cấp đối tác", "Người lập phiếu", "Ngày nhập hàng", "Tổng tiền thanh toán", "Trạng thái" };
            int headerRow = 4;
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            worksheet.Row(headerRow).Height = 28;

            int currentRow = 5;
            int stt = 1;
            decimal totalAll = 0;

            foreach (var item in list)
            {
                var tongTien = (item.TongTien.HasValue && item.TongTien.Value > 0)
                    ? item.TongTien.Value
                    : (item.ChiTietPhieuNhaps?.Sum(c => c.ThanhTien ?? (c.SoLuong * c.DonGia) ?? 0) ?? 0);
                totalAll += tongTien;

                worksheet.Cell(currentRow, 1).Value = stt++;
                worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 2).Value = item.MaPhieuNhapCode ?? $"PN{item.MaPhieuNhap:D4}";
                worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 2).Style.Font.FontName = "Consolas";

                worksheet.Cell(currentRow, 3).Value = item.NhaCungCap?.TenNhaCungCap ?? "N/A";
                worksheet.Cell(currentRow, 3).Style.Font.Bold = true;

                worksheet.Cell(currentRow, 4).Value = item.NguoiDung?.TenDangNhap ?? "Hệ thống";
                worksheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 5).Value = item.NgayNhap?.ToString("dd/MM/yyyy") ?? "—";
                worksheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 6).Value = tongTien;
                worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0 \"₫\"";
                worksheet.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                var statusCell = worksheet.Cell(currentRow, 7);
                if (item.TrangThai == "xacnhan")
                {
                    statusCell.Value = "Đã xác nhận";
                    statusCell.Style.Font.FontColor = XLColor.FromHtml("#059669");
                    statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D1FAE5");
                }
                else if (item.TrangThai == "huy")
                {
                    statusCell.Value = "Đã hủy";
                    statusCell.Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                    statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEE2E2");
                }
                else
                {
                    statusCell.Value = "Chờ xử lý / Nháp";
                    statusCell.Style.Font.FontColor = XLColor.FromHtml("#D97706");
                    statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                }
                statusCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (currentRow % 2 == 0)
                {
                    worksheet.Range(currentRow, 1, currentRow, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }
                currentRow++;
            }

            // Tổng kết
            worksheet.Cell(currentRow, 1).Value = "TỔNG CỘNG TẤT CẢ PHIẾU";
            worksheet.Range(currentRow, 1, currentRow, 5).Merge();
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 6).Value = totalAll;
            worksheet.Cell(currentRow, 6).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0 \"₫\"";

            var totalRange = worksheet.Range(currentRow, 1, currentRow, 7);
            totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
            totalRange.Style.Font.Bold = true;

            var dataTableRange = worksheet.Range(4, 1, currentRow, 7);
            dataTableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            dataTableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            worksheet.Columns().AdjustToContents();
            worksheet.Column(3).Width = 35;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            var fileName = $"DanhSachPhieuNhap_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: PhieuNhap/ExportChiTietExcel/5
        public async Task<IActionResult> ExportChiTietExcel(int id)
        {
            var phieuNhap = await _context.PhieuNhaps
                .Include(p => p.NhaCungCap)
                .Include(p => p.NguoiDung)
                .Include(p => p.ChiTietPhieuNhaps)
                    .ThenInclude(c => c.SanPham)
                        .ThenInclude(s => s!.DonViTinh)
                .FirstOrDefaultAsync(p => p.MaPhieuNhap == id);

            if (phieuNhap == null)
            {
                return NotFound();
            }

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("ChiTietPhieuNhap");

            // Header Công ty
            worksheet.Cell("A1").Value = "HỆ THỐNG QUẢN LÝ NHẬP HÀNG & LỰA CHỌN NHÀ CUNG CẤP AHP";
            worksheet.Range("A1:G1").Merge();
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 14;
            worksheet.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#1E293B");
            worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell("A2").Value = $"PHIẾU NHẬP HÀNG: {phieuNhap.MaPhieuNhapCode ?? $"PN{phieuNhap.MaPhieuNhap:D4}"}";
            worksheet.Range("A2:G2").Merge();
            worksheet.Cell("A2").Style.Font.Bold = true;
            worksheet.Cell("A2").Style.Font.FontSize = 16;
            worksheet.Cell("A2").Style.Font.FontColor = XLColor.FromHtml("#2563EB");
            worksheet.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Thông tin phiếu
            worksheet.Cell("A4").Value = "Nhà cung cấp:";
            worksheet.Cell("A4").Style.Font.Bold = true;
            worksheet.Cell("B4").Value = phieuNhap.NhaCungCap?.TenNhaCungCap ?? "N/A";
            worksheet.Range("B4:D4").Merge();
            worksheet.Cell("B4").Style.Font.Bold = true;

            worksheet.Cell("E4").Value = "Mã đối tác:";
            worksheet.Cell("E4").Style.Font.Bold = true;
            worksheet.Cell("F4").Value = phieuNhap.NhaCungCap?.MaNhaCungCapCode ?? $"NCC{phieuNhap.MaNhaCungCap:D3}";

            worksheet.Cell("A5").Value = "Địa chỉ NCC:";
            worksheet.Cell("A5").Style.Font.Bold = true;
            worksheet.Cell("B5").Value = phieuNhap.NhaCungCap?.DiaChi ?? "—";
            worksheet.Range("B5:D5").Merge();

            worksheet.Cell("E5").Value = "SĐT liên hệ:";
            worksheet.Cell("E5").Style.Font.Bold = true;
            worksheet.Cell("F5").Value = phieuNhap.NhaCungCap?.SoDienThoai ?? "—";

            worksheet.Cell("A6").Value = "Ngày lập phiếu:";
            worksheet.Cell("A6").Style.Font.Bold = true;
            worksheet.Cell("B6").Value = phieuNhap.NgayTao?.ToString("dd/MM/yyyy HH:mm") ?? "—";

            worksheet.Cell("C6").Value = "Ngày nhập:";
            worksheet.Cell("C6").Style.Font.Bold = true;
            worksheet.Cell("D6").Value = phieuNhap.NgayNhap?.ToString("dd/MM/yyyy") ?? "—";

            worksheet.Cell("E6").Value = "Trạng thái:";
            worksheet.Cell("E6").Style.Font.Bold = true;
            worksheet.Cell("F6").Value = phieuNhap.TrangThai == "xacnhan" ? "Đã xác nhận" : (phieuNhap.TrangThai == "huy" ? "Đã hủy" : "Chờ xử lý / Nháp");

            // Bảng chi tiết mặt hàng
            var headers = new string[] { "STT", "Mã hàng", "Tên hàng hóa / Sản phẩm", "ĐVT", "Số lượng", "Đơn giá nhập", "Thành tiền" };
            int headerRow = 8;
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            worksheet.Row(headerRow).Height = 26;

            int currentRow = 9;
            int stt = 1;
            int tongSoLuong = 0;
            decimal tongThanhTien = 0;

            foreach (var ct in phieuNhap.ChiTietPhieuNhaps)
            {
                var qty = ct.SoLuong ?? 0;
                var donGia = ct.DonGia ?? 0;
                var thanhTien = ct.ThanhTien ?? (qty * donGia);

                tongSoLuong += qty;
                tongThanhTien += thanhTien;

                worksheet.Cell(currentRow, 1).Value = stt++;
                worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 2).Value = ct.SanPham?.MaSanPhamCode ?? $"SP{ct.MaSanPham:D3}";
                worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 2).Style.Font.FontName = "Consolas";

                worksheet.Cell(currentRow, 3).Value = ct.SanPham?.TenSanPham ?? "N/A";
                worksheet.Cell(currentRow, 3).Style.Font.Bold = true;

                worksheet.Cell(currentRow, 4).Value = ct.SanPham?.DonViTinh?.TenDonVi ?? "—";
                worksheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 5).Value = qty;
                worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell(currentRow, 6).Value = donGia;
                worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0 \"₫\"";
                worksheet.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell(currentRow, 7).Value = thanhTien;
                worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0 \"₫\"";
                worksheet.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                if (currentRow % 2 == 0)
                {
                    worksheet.Range(currentRow, 1, currentRow, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }
                currentRow++;
            }

            // Hàng tổng tiền
            worksheet.Cell(currentRow, 1).Value = "TỔNG CỘNG TIỀN THANH TOÁN";
            worksheet.Range(currentRow, 1, currentRow, 4).Merge();
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 5).Value = tongSoLuong;
            worksheet.Cell(currentRow, 5).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 7).Value = tongThanhTien;
            worksheet.Cell(currentRow, 7).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0 \"₫\"";
            worksheet.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var tableTotalRange = worksheet.Range(currentRow, 1, currentRow, 7);
            tableTotalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
            tableTotalRange.Style.Font.Bold = true;

            var tableBorder = worksheet.Range(8, 1, currentRow, 7);
            tableBorder.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            tableBorder.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Chữ ký xác nhận bên dưới
            int signRow = currentRow + 3;
            worksheet.Cell(signRow, 2).Value = "Người Lập Phiếu";
            worksheet.Cell(signRow, 2).Style.Font.Bold = true;
            worksheet.Cell(signRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(signRow, 4).Value = "Thủ Kho";
            worksheet.Cell(signRow, 4).Style.Font.Bold = true;
            worksheet.Cell(signRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(signRow, 6).Value = "Đại Diện Nhà Cung Cấp";
            worksheet.Cell(signRow, 6).Style.Font.Bold = true;
            worksheet.Cell(signRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(signRow + 1, 2).Value = "(Ký, họ tên)";
            worksheet.Cell(signRow + 1, 2).Style.Font.Italic = true;
            worksheet.Cell(signRow + 1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(signRow + 1, 4).Value = "(Ký, họ tên)";
            worksheet.Cell(signRow + 1, 4).Style.Font.Italic = true;
            worksheet.Cell(signRow + 1, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(signRow + 1, 6).Value = "(Ký, đóng dấu)";
            worksheet.Cell(signRow + 1, 6).Style.Font.Italic = true;
            worksheet.Cell(signRow + 1, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Columns().AdjustToContents();
            worksheet.Column(3).Width = 35;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            var fileName = $"PhieuNhap_{phieuNhap.MaPhieuNhapCode ?? phieuNhap.MaPhieuNhap.ToString()}_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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
