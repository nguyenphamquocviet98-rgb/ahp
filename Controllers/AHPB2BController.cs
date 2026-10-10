using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;
using code.Services;
using System.Security.Claims;

namespace code.Controllers
{
    [Authorize]
    public class AHPB2BController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AHPB2BService _ahpB2BService;
        private readonly AiAdvisorService _aiAdvisorService;

        public AHPB2BController(ApplicationDbContext context, AHPB2BService ahpB2BService, AiAdvisorService aiAdvisorService)
        {
            _context = context;
            _ahpB2BService = ahpB2BService;
            _aiAdvisorService = aiAdvisorService;
        }

        private int GetCurrentMaDoanhNghiep()
        {
            var claim = User.FindFirstValue("MaDoanhNghiep");
            int.TryParse(claim, out int dnId);
            return dnId;
        }

        /// <summary>
        /// Bước 1: Danh sách sản phẩm có nhiều NPP chào bán trên sàn B2B (Đủ điều kiện so sánh AHP)
        /// </summary>
        public async Task<IActionResult> Index()
        {
            int currentDNId = GetCurrentMaDoanhNghiep();
            if (!User.IsCompanyMember() && !User.IsPlatformAdmin()) return Forbid();

            // Lấy các sản phẩm đang có từ 2 NPP khác nhau chào bán (loại trừ chính NPP hiện tại)
            var query = _context.TonKhoNPPs
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DanhMuc)
                .Include(t => t.SanPham)
                    .ThenInclude(s => s!.DonViTinh)
                .Include(t => t.DoanhNghiep)
                .Where(t => t.DangChaoBan && t.SoLuongTon > 0 && t.DoanhNghiep != null && t.DoanhNghiep.TrangThai);

            if (currentDNId > 0)
            {
                query = query.Where(t => t.MaDoanhNghiep != currentDNId);
            }

            var allKho = await query.ToListAsync();

            // Nhóm theo sản phẩm
            var grouped = allKho
                .GroupBy(t => t.SanPham)
                .Where(g => g.Key != null)
                .Select(g => new
                {
                    SanPham = g.Key,
                    SoNPPBan = g.Select(x => x.MaDoanhNghiep).Distinct().Count(),
                    GiaThapNhat = g.Min(x => x.GiaSi),
                    GiaCaoNhat = g.Max(x => x.GiaSi),
                    GiaoNhanhNhat = g.Min(x => x.ThoiGianGiaoHang),
                    CoGianHangMall = g.Any(x => x.DoanhNghiep!.LaGianHangMall),
                    DanhSachNPP = g.Select(x => x.DoanhNghiep!.TenDoanhNghiep).ToList()
                })
                .OrderByDescending(x => x.SoNPPBan)
                .ToList();

            ViewBag.CurrentDNId = currentDNId;
            return View(grouped);
        }

        /// <summary>
        /// Bước 2: Thiết lập ma trận cặp tiêu chí hoặc chọn kịch bản ưu tiên
        /// </summary>
        [Authorize(Roles = "chu_npp,quanly")]
        public async Task<IActionResult> ThietLap(int maSanPham, int soLuong = 20, string kieuUuTien = "can_bang")
        {
            var sp = await _context.SanPhams
                .Include(s => s!.DanhMuc)
                .Include(s => s!.DonViTinh)
                .FirstOrDefaultAsync(s => s.MaSanPham == maSanPham);

            if (sp == null) return NotFound();

            int currentDNId = GetCurrentMaDoanhNghiep();
            if (!User.IsCompanyMember() && !User.IsPlatformAdmin()) return Forbid();

            // Lấy danh sách các NPP đối thủ đang chào bán SP này
            var candidates = await _context.TonKhoNPPs
                .Include(t => t.DoanhNghiep)
                .Where(t => t.MaSanPham == maSanPham && t.DangChaoBan && t.SoLuongTon > 0 && t.DoanhNghiep != null && t.DoanhNghiep.TrangThai && (currentDNId == 0 || t.MaDoanhNghiep != currentDNId))
                .ToListAsync();

            ViewBag.SanPham = sp;
            ViewBag.SoLuong = soLuong;
            ViewBag.KieuUuTien = kieuUuTien;
            ViewBag.Candidates = candidates;

            // Ma trận mặc định theo kịch bản
            var matrix = _ahpB2BService.GetDefaultMatrix(kieuUuTien);
            ViewBag.Matrix = matrix;

            return View(sp);
        }

        /// <summary>
        /// Bước 3: Chạy thuật toán AHP và lưu kết quả phiên
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "chu_npp,quanly")]
        public async Task<IActionResult> ChayAHP(int maSanPham, int soLuong, string kieuUuTien, double[]? comparisons)
        {
            var sp = await _context.SanPhams.FindAsync(maSanPham);
            if (sp == null) return NotFound();

            int currentDNId = GetCurrentMaDoanhNghiep();
            if (!User.IsCompanyMember() && !User.IsPlatformAdmin()) return Forbid();
            if (!User.IsCompanyManager()) return Forbid();
            if (!ModelState.IsValid || soLuong <= 0) return BadRequest("Số lượng và ma trận không hợp lệ.");
            var candidates = await _context.TonKhoNPPs
                .Include(t => t.DoanhNghiep)
                .Where(t => t.MaSanPham == maSanPham && t.DangChaoBan && t.SoLuongTon >= soLuong && t.GiaSi > 0 && t.ThoiGianGiaoHang > 0 && t.DoanhNghiep != null && t.DoanhNghiep.TrangThai && (currentDNId == 0 || t.MaDoanhNghiep != currentDNId))
                .ToListAsync();

            if (!candidates.Any())
            {
                TempData["ErrorMessage"] = "Không có nhà cung cấp đủ số lượng yêu cầu với giá và thời gian giao hợp lệ.";
                return RedirectToAction("Index");
            }

            // Lấy ma trận theo kịch bản người dùng chọn
            var matrix = _ahpB2BService.GetDefaultMatrix(kieuUuTien);

            if (comparisons != null && comparisons.Length != 6)
                return BadRequest("Cần đủ 6 cặp so sánh giữa 4 tiêu chí.");
            if (comparisons?.Length == 6)
            {
                int position = 0;
                for (int i = 0; i < 4; i++)
                for (int j = i + 1; j < 4; j++)
                {
                    var value = comparisons[position++];
                    if (!double.IsFinite(value) || value < 1.0 / 9 - 1e-6 || value > 9)
                        return BadRequest("So sánh cặp phải nằm trong khoảng 1/9 đến 9.");
                    matrix[i, j] = value;
                    matrix[j, i] = 1 / value;
                }
            }
            // Chạy AHP Service
            var evalResult = _ahpB2BService.Evaluate(candidates, matrix, sp.TenSanPham ?? "Sản phẩm", soLuong);

            if (!evalResult.AhpResult.IsConsistent)
            {
                TempData["ErrorMessage"] = $"CR = {evalResult.AhpResult.CR:F4} > 0,10. Hãy điều chỉnh ma trận trước khi xếp hạng.";
                await ThietLap(maSanPham, soLuong, kieuUuTien);
                ViewBag.Matrix = matrix;
                return View("ThietLap", sp);
            }
            // Lưu vào CSDL phiên AHP
            var phien = new PhienAHP_B2B
            {
                MaNPPMua = currentDNId,
                SnapshotJson = JsonSerializer.Serialize(AhpSnapshot.From(evalResult)),
                MaSanPham = maSanPham,
                SoLuongCanMua = soLuong,
                NgayTao = DateTime.Now,
                TrangThai = "hoan_thanh",
                MaNPPDeXuat = evalResult.TopCandidate?.MaDoanhNghiep,
                GhiChu = $"AHP Kịch bản: {kieuUuTien} | CR={evalResult.AhpResult.CR:F4}"
            };



            // Lưu chi tiết kết quả từng NPP
            foreach (var cand in evalResult.Candidates)
            {
                phien.KetQuaAHPs.Add(new KetQuaAHP_B2B
                {

                    MaDoanhNghiep = cand.MaDoanhNghiep,
                    DiemGia = (decimal)cand.DiemGia,
                    DiemGiaoHang = (decimal)cand.DiemGiaoHang,
                    DiemUyTin = (decimal)cand.DiemUyTin,
                    DiemTonKho = (decimal)cand.DiemTonKho,
                    DiemTongHop = (decimal)cand.DiemTongHop,
                    XepHang = cand.XepHang,
                    DeXuat = cand.DeXuat
                });
            }
            _context.PhienAHP_B2Bs.Add(phien);
            await _context.SaveChangesAsync();

            return RedirectToAction("KetQua", new { id = phien.MaPhien });
        }

        /// <summary>
        /// Bước 4: Hiển thị Báo cáo Phân tích AHP & Khuyến nghị đối tác tối ưu
        /// </summary>
        public async Task<IActionResult> KetQua(int id)
        {
            var phien = await _context.PhienAHP_B2Bs
                .Include(p => p.SanPham)
                    .ThenInclude(s => s!.DonViTinh)
                .Include(p => p.NPPDeXuat)
                .Include(p => p.KetQuaAHPs)
                    .ThenInclude(k => k.DoanhNghiep)
                .FirstOrDefaultAsync(p => p.MaPhien == id);

            if (phien == null) return NotFound();
            if (!User.IsPlatformAdmin() && (!User.IsCompanyMember() || phien.MaNPPMua != User.CompanyId())) return Forbid();

            // Lấy thêm thông tin tồn kho & giá sỉ hiện tại của các NPP đó
            var maNppList = phien.KetQuaAHPs.Select(k => k.MaDoanhNghiep).ToList();
            var khoList = await _context.TonKhoNPPs
                .Where(t => t.MaSanPham == phien.MaSanPham && maNppList.Contains(t.MaDoanhNghiep))
                .ToListAsync();

            var snapshot = AhpSnapshot.Read(phien.SnapshotJson);
            ViewBag.Snapshot = snapshot;
            ViewBag.CurrentStock = khoList.ToDictionary(k => k.MaDoanhNghiep, k => k);
            ViewBag.KhoDict = snapshot?.ToStock(phien.MaSanPham) ?? new Dictionary<int, TonKhoNPP>();
            if (snapshot != null)
            {
                phien.SanPham!.TenSanPham = snapshot.ProductName;
                foreach (var result in phien.KetQuaAHPs)
                    result.DoanhNghiep = snapshot.ToStock(phien.MaSanPham).GetValueOrDefault(result.MaDoanhNghiep)?.DoanhNghiep;
            }

            return View(phien);
        }

        /// <summary>
        /// Bước 5 (Giai đoạn 6): Gọi AI Decision Advisor phân tích đa chiều & chiến lược đàm phán
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TuVanAI(int id)
        {
            var phien = await _context.PhienAHP_B2Bs
                .Include(p => p.SanPham)
                    .ThenInclude(s => s!.DonViTinh)
                .Include(p => p.NPPDeXuat)
                .Include(p => p.KetQuaAHPs)
                    .ThenInclude(k => k.DoanhNghiep)
                .FirstOrDefaultAsync(p => p.MaPhien == id);

            if (phien == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phiên phân tích AHP!" });
            }

            if (!User.IsPlatformAdmin() && (!User.IsCompanyMember() || phien.MaNPPMua != User.CompanyId())) return Forbid();
            var snapshot = AhpSnapshot.Read(phien.SnapshotJson);
            if (snapshot == null) return Json(new { success = false, message = "Phiên cũ chưa lưu dữ liệu đầu vào. Hãy tạo phiên đánh giá mới." });
            phien.SanPham!.TenSanPham = snapshot.ProductName;
            var khoDict = snapshot.ToStock(phien.MaSanPham);
            foreach (var result in phien.KetQuaAHPs) result.DoanhNghiep = khoDict.GetValueOrDefault(result.MaDoanhNghiep)?.DoanhNghiep;
            var aiResult = await _aiAdvisorService.AnalyzeAhpDecisionAsync(phien, khoDict);

            return Json(aiResult);
        }
    }
}
