using code.Services;
using System.IO;
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
    public class TonKhoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TonKhoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TonKho
        public async Task<IActionResult> Index(string? search, int? maDanhMuc, string? mucTon)
        {
            if (User.CompanyId() > 0) return RedirectToAction("Index", "KhoNPP", new { search });
            var query = _context.TonKhos
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DanhMuc)
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DonViTinh)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(t => t.SanPham != null &&
                                         ((t.SanPham.TenSanPham != null && t.SanPham.TenSanPham.Contains(s)) ||
                                          (t.SanPham.MaSanPhamCode != null && t.SanPham.MaSanPhamCode.Contains(s))));
                ViewData["Search"] = s;
            }

            if (maDanhMuc.HasValue && maDanhMuc.Value > 0)
            {
                query = query.Where(t => t.SanPham != null && t.SanPham.MaDanhMuc == maDanhMuc.Value);
                ViewData["SelectedDanhMuc"] = maDanhMuc.Value;
            }

            if (!string.IsNullOrWhiteSpace(mucTon))
            {
                if (mucTon == "thap")
                {
                    query = query.Where(t => (t.SoLuongTon ?? 0) < 20);
                }
                else if (mucTon == "antoan")
                {
                    query = query.Where(t => (t.SoLuongTon ?? 0) >= 20);
                }
                ViewData["SelectedMucTon"] = mucTon;
            }

            ViewBag.DanhMucs = new SelectList(await _context.DanhMucs.OrderBy(d => d.TenDanhMuc).ToListAsync(), "MaDanhMuc", "TenDanhMuc", maDanhMuc);

            var list = await query.OrderBy(t => t.SoLuongTon).ToListAsync();

            // Tính toán tổng quan kho
            ViewBag.TongSoMatHang = await _context.TonKhos.CountAsync();
            ViewBag.TongSoLuongTon = await _context.TonKhos.SumAsync(t => t.SoLuongTon ?? 0);
            ViewBag.SoMatHangTonThap = await _context.TonKhos.CountAsync(t => (t.SoLuongTon ?? 0) < 20);
            ViewBag.TongGiaTriTonKho = await _context.TonKhos
                .Include(t => t.SanPham)
                .SumAsync(t => (decimal)(t.SoLuongTon ?? 0) * (t.SanPham != null ? (t.SanPham.GiaMacDinh ?? 0) : 0));

            return View(list);
        }

        // GET: TonKho/ExportExcel
        public async Task<IActionResult> ExportExcel(string? search, int? maDanhMuc, string? mucTon)
        {
            if (User.CompanyId() > 0) return RedirectToAction("Index", "KhoNPP", new { search });
            var query = _context.TonKhos
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DanhMuc)
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DonViTinh)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(t => t.SanPham != null &&
                                         ((t.SanPham.TenSanPham != null && t.SanPham.TenSanPham.Contains(s)) ||
                                          (t.SanPham.MaSanPhamCode != null && t.SanPham.MaSanPhamCode.Contains(s))));
            }

            if (maDanhMuc.HasValue && maDanhMuc.Value > 0)
            {
                query = query.Where(t => t.SanPham != null && t.SanPham.MaDanhMuc == maDanhMuc.Value);
            }

            if (!string.IsNullOrWhiteSpace(mucTon))
            {
                if (mucTon == "thap")
                {
                    query = query.Where(t => (t.SoLuongTon ?? 0) < 20);
                }
                else if (mucTon == "antoan")
                {
                    query = query.Where(t => (t.SoLuongTon ?? 0) >= 20);
                }
            }

            var list = await query.OrderBy(t => t.SoLuongTon).ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("BaoCaoTonKho");

            // 1. Tiêu đề báo cáo
            worksheet.Cell("A1").Value = "BÁO CÁO TỒN KHO HÀNG HÓA & ĐỊNH MỨC AN TOÀN";
            worksheet.Range("A1:I1").Merge();
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 16;
            worksheet.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#0F172A");
            worksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell("A2").Value = $"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Người xuất: {User.Identity?.Name ?? "Admin"}";
            worksheet.Range("A2:I2").Merge();
            worksheet.Cell("A2").Style.Font.Italic = true;
            worksheet.Cell("A2").Style.Font.FontSize = 11;
            worksheet.Cell("A2").Style.Font.FontColor = XLColor.FromHtml("#64748B");
            worksheet.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // 2. Header bảng
            var headers = new string[]
            {
                "STT", "Mã SP", "Tên sản phẩm", "Danh mục", "Đơn vị tính", "Số lượng tồn", "Đơn giá định mức", "Tổng giá trị tồn", "Trạng thái cảnh báo"
            };

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

            // 3. Đổ dữ liệu
            int currentRow = 5;
            int stt = 1;
            int tongQty = 0;
            decimal tongTien = 0;

            foreach (var item in list)
            {
                var qty = item.SoLuongTon ?? 0;
                var gia = item.SanPham?.GiaMacDinh ?? 0;
                var thanhTien = qty * gia;
                tongQty += qty;
                tongTien += thanhTien;

                worksheet.Cell(currentRow, 1).Value = stt++;
                worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 2).Value = item.SanPham?.MaSanPhamCode ?? $"SP{item.MaSanPham:D3}";
                worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, 2).Style.Font.FontName = "Consolas";

                worksheet.Cell(currentRow, 3).Value = item.SanPham?.TenSanPham ?? "N/A";
                worksheet.Cell(currentRow, 3).Style.Font.Bold = true;

                worksheet.Cell(currentRow, 4).Value = item.SanPham?.DanhMuc?.TenDanhMuc ?? "Chưa phân loại";
                worksheet.Cell(currentRow, 5).Value = item.SanPham?.DonViTinh?.TenDonVi ?? "—";
                worksheet.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(currentRow, 6).Value = qty;
                worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell(currentRow, 7).Value = gia;
                worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0 \"₫\"";
                worksheet.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                worksheet.Cell(currentRow, 8).Value = thanhTien;
                worksheet.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0 \"₫\"";
                worksheet.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // Trạng thái cảnh báo
                var statusCell = worksheet.Cell(currentRow, 9);
                if (qty <= 0)
                {
                    statusCell.Value = "HẾT HÀNG";
                    statusCell.Style.Font.Bold = true;
                    statusCell.Style.Font.FontColor = XLColor.FromHtml("#EF4444");
                    statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEE2E2");
                }
                else if (qty < 20)
                {
                    statusCell.Value = "TỒN THẤP (< 20)";
                    statusCell.Style.Font.Bold = true;
                    statusCell.Style.Font.FontColor = XLColor.FromHtml("#D97706");
                    statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                }
                else
                {
                    statusCell.Value = "AN TOÀN";
                    statusCell.Style.Font.FontColor = XLColor.FromHtml("#059669");
                    statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D1FAE5");
                }
                statusCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Alternate row colors
                if (currentRow % 2 == 0)
                {
                    worksheet.Range(currentRow, 1, currentRow, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }

                currentRow++;
            }

            // 4. Hàng tổng kết
            worksheet.Cell(currentRow, 1).Value = "TỔNG CỘNG";
            worksheet.Range(currentRow, 1, currentRow, 5).Merge();
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(currentRow, 6).Value = tongQty;
            worksheet.Cell(currentRow, 6).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0";

            worksheet.Cell(currentRow, 8).Value = tongTien;
            worksheet.Cell(currentRow, 8).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0 \"₫\"";

            var totalRange = worksheet.Range(currentRow, 1, currentRow, 9);
            totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
            totalRange.Style.Font.Bold = true;

            // Viền bảng (Borders)
            var dataTableRange = worksheet.Range(4, 1, currentRow, 9);
            dataTableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            dataTableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataTableRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#94A3B8");
            dataTableRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#CBD5E1");

            worksheet.Columns().AdjustToContents();
            worksheet.Column(1).Width = 8;
            worksheet.Column(3).Width = 35;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            var fileName = $"BaoCaoTonKho_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: TonKho/DieuChinhKho — nếu user truy cập GET, redirect về trang chính
        [HttpGet]
        public IActionResult DieuChinhKho()
        {
            return RedirectToAction(nameof(Index));
        }

        // POST: TonKho/DieuChinhKho
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> DieuChinhKho(int maSanPham, string loaiDieuChinh, int soLuongThayDoi, string? lyDo)
        {
            if (!User.IsPlatformAdmin()) return Forbid();
            try
            {
                if (soLuongThayDoi <= 0)
                {
                    TempData["ErrorMessage"] = "Số lượng thay đổi phải lớn hơn 0.";
                    return RedirectToAction(nameof(Index));
                }

                var tonKho = await _context.TonKhos
                    .Include(t => t.SanPham)
                    .FirstOrDefaultAsync(t => t.MaSanPham == maSanPham);

                if (tonKho == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy dữ liệu tồn kho của sản phẩm này.";
                    return RedirectToAction(nameof(Index));
                }

                int delta = 0;
                string tenSp = tonKho.SanPham?.TenSanPham ?? "Sản phẩm";

                if (loaiDieuChinh == "giam")
                {
                    if ((tonKho.SoLuongTon ?? 0) < soLuongThayDoi)
                    {
                        TempData["ErrorMessage"] = $"Số lượng giảm ({soLuongThayDoi}) không thể lớn hơn số lượng tồn hiện có ({tonKho.SoLuongTon ?? 0}).";
                        return RedirectToAction(nameof(Index));
                    }

                    delta = -soLuongThayDoi;
                    tonKho.SoLuongTon = (tonKho.SoLuongTon ?? 0) - soLuongThayDoi;
                }
                else
                {
                    delta = soLuongThayDoi;
                    tonKho.SoLuongTon = (tonKho.SoLuongTon ?? 0) + soLuongThayDoi;
                }

                tonKho.NgayCapNhat = DateTime.Now;

                // Ghi nhận vào Lịch sử biến động kho
                var lichSu = new LichSuNhapHang
                {
                    MaSanPham = maSanPham,
                    SoLuongThayDoi = delta,
                    LoaiThayDoi = "dieuchinh",
                    NgayTao = DateTime.Now
                };

                _context.LichSuNhapHangs.Add(lichSu);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã {(delta < 0 ? "giảm (xuất)" : "tăng")} {Math.Abs(delta)} đơn vị cho [{tenSp}] thành công! Lý do: {(string.IsNullOrWhiteSpace(lyDo) ? "Điều chỉnh kho" : lyDo)}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi điều chỉnh kho: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
