using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using code.Data;
using code.Models;
using code.Models.ViewModels;
using code.Services;

namespace code.Controllers
{
    [Authorize]
    public class AHPController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AHPService _ahpService;

        public AHPController(ApplicationDbContext context, AHPService ahpService)
        {
            _context = context;
            _ahpService = ahpService;
        }

        // ==========================================
        // 1. DANH SÁCH & TẠO KỲ ĐÁNH GIÁ (Mục 25)
        // ==========================================

        // GET: AHP/Index
        public async Task<IActionResult> Index()
        {
            var list = await _context.KyDanhGias
                .Include(k => k.NguoiDung)
                .Include(k => k.TrongSoTieuChis)
                .Include(k => k.KetQuaXepHangs)
                    .ThenInclude(kq => kq.NhaCungCap)
                .OrderByDescending(k => k.MaKyDanhGia)
                .ToListAsync();

            return View(list);
        }

        // GET: AHP/CreateKy
        [Authorize(Roles = "admin,quanly")]
        public IActionResult CreateKy()
        {
            var model = new KyDanhGia
            {
                TenKyDanhGia = $"Đánh giá Nhà cung cấp {DateTime.Now:MM/yyyy}",
                NgayTao = DateTime.Now,
                TrangThai = "khoitao"
            };
            return View(model);
        }

        // POST: AHP/CreateKy
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> CreateKy(KyDanhGia model)
        {
            if (string.IsNullOrWhiteSpace(model.TenKyDanhGia))
            {
                ModelState.AddModelError("TenKyDanhGia", "Tên kỳ đánh giá không được để trống.");
            }

            if (ModelState.IsValid)
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("MaNguoiDung")?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    model.MaNguoiDung = userId;
                }

                model.NgayTao = DateTime.Now;
                model.TrangThai = "khoitao";

                _context.KyDanhGias.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Tạo kỳ đánh giá '{model.TenKyDanhGia}' thành công! Bước tiếp theo: Nhập ma trận so sánh cặp tiêu chí.";
                return RedirectToAction(nameof(MaTranSoSanh), new { id = model.MaKyDanhGia });
            }

            return View(model);
        }

        // ==========================================
        // 2. MA TRẬN SO SÁNH CẶP TIÊU CHÍ (Mục 26 -> 33)
        // ==========================================

        // GET: AHP/MaTranSoSanh/5
        public async Task<IActionResult> MaTranSoSanh(int? id)
        {
            // Nếu không truyền ID, lấy kỳ mới nhất hoặc đầu tiên
            if (id == null)
            {
                var latestKy = await _context.KyDanhGias.OrderByDescending(k => k.MaKyDanhGia).FirstOrDefaultAsync();
                if (latestKy == null)
                {
                    return RedirectToAction(nameof(CreateKy));
                }
                id = latestKy.MaKyDanhGia;
            }

            var ky = await _context.KyDanhGias.FindAsync(id);
            if (ky == null)
            {
                return NotFound();
            }

            var tieuChis = await _context.TieuChis.OrderBy(t => t.MaTieuChi).ToListAsync();
            int n = tieuChis.Count;

            // Lấy ma trận đã lưu nếu có
            var existingMatrix = await _context.MaTranSoSanhs
                .Where(m => m.MaKyDanhGia == id)
                .ToListAsync();

            double[][] matrix = new double[n][];
            for (int i = 0; i < n; i++)
            {
                matrix[i] = new double[n];
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        matrix[i][j] = 1.0;
                    }
                    else
                    {
                        var rowCriteriaId = tieuChis[i].MaTieuChi;
                        var colCriteriaId = tieuChis[j].MaTieuChi;
                        var cell = existingMatrix.FirstOrDefault(m => m.MaTieuChiDong == rowCriteriaId && m.MaTieuChiCot == colCriteriaId);
                        matrix[i][j] = cell?.GiaTri.HasValue == true ? (double)cell.GiaTri.Value : 1.0;
                    }
                }
            }

            var viewModel = new MaTranSoSanhInputModel
            {
                MaKyDanhGia = ky.MaKyDanhGia,
                TenKyDanhGia = ky.TenKyDanhGia ?? $"Kỳ #{ky.MaKyDanhGia}",
                DanhSachTieuChi = tieuChis,
                MaTran = matrix,
                TrongSos = await _context.TrongSoTieuChis
                    .Include(t => t.TieuChi)
                    .Where(t => t.MaKyDanhGia == id)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: AHP/LuuVaTinhToanAHP
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> LuuVaTinhToanAHP(int maKyDanhGia, double[][] matrix)
        {
            var ky = await _context.KyDanhGias.FindAsync(maKyDanhGia);
            if (ky == null)
            {
                TempData["ErrorMessage"] = "Kỳ đánh giá không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            var tieuChis = await _context.TieuChis.OrderBy(t => t.MaTieuChi).ToListAsync();
            int n = tieuChis.Count;

            // Xây dựng ma trận double[,] an toàn với CultureInfo Invariant
            double[,] mat = new double[n, n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        mat[i, j] = 1.0;
                    }
                    else if (j > i)
                    {
                        string formKey = $"matrix[{i}][{j}]";
                        string? rawVal = Request.Form[formKey].FirstOrDefault();
                        if (!string.IsNullOrEmpty(rawVal) && double.TryParse(rawVal.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsed) && parsed > 0)
                        {
                            mat[i, j] = parsed;
                        }
                        else if (matrix != null && i < matrix.Length && j < matrix[i].Length && matrix[i][j] > 0 && matrix[i][j] <= 9.0)
                        {
                            mat[i, j] = matrix[i][j];
                        }
                        else
                        {
                            mat[i, j] = 1.0;
                        }
                    }
                }
            }

            // Đảm bảo tính đối xứng nghịch đảo tuyệt đối a[j, i] = 1 / a[i, j]
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    mat[j, i] = 1.0 / mat[i, j];
                }
            }

            // 1. Lưu vào bảng MaTranSoSanh
            var oldMatrixCells = await _context.MaTranSoSanhs.Where(m => m.MaKyDanhGia == maKyDanhGia).ToListAsync();
            if (oldMatrixCells.Any())
            {
                _context.MaTranSoSanhs.RemoveRange(oldMatrixCells);
            }

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    _context.MaTranSoSanhs.Add(new MaTranSoSanh
                    {
                        MaKyDanhGia = maKyDanhGia,
                        MaTieuChiDong = tieuChis[i].MaTieuChi,
                        MaTieuChiCot = tieuChis[j].MaTieuChi,
                        GiaTri = (decimal)Math.Round(mat[i, j], 3)
                    });
                }
            }

            // 2. Chạy thuật toán AHP (Backend AHPService)
            var ahpResult = _ahpService.CalculateAHP(mat);

            // 3. Kiểm tra tính nhất quán CR <= 0.10
            if (!ahpResult.IsConsistent)
            {
                await _context.SaveChangesAsync();
                TempData["ErrorMessage"] = $"Ma trận so sánh KHÔNG ĐẠT tính nhất quán! Tỷ số CR = {ahpResult.CR:0.0000} (> 0.10). Vui lòng điều chỉnh lại mức độ ưu tiên giữa các tiêu chí.";
                return RedirectToAction(nameof(MaTranSoSanh), new { id = maKyDanhGia });
            }

            // 4. Lưu kết quả trọng số vào bảng TrongSoTieuChi
            var oldTrongSos = await _context.TrongSoTieuChis.Where(t => t.MaKyDanhGia == maKyDanhGia).ToListAsync();
            if (oldTrongSos.Any())
            {
                _context.TrongSoTieuChis.RemoveRange(oldTrongSos);
            }

            for (int i = 0; i < n; i++)
            {
                _context.TrongSoTieuChis.Add(new TrongSoTieuChi
                {
                    MaKyDanhGia = maKyDanhGia,
                    MaTieuChi = tieuChis[i].MaTieuChi,
                    TrongSo = (decimal)Math.Round(ahpResult.TrongSo[i], 3),
                    LambdaMax = (decimal)Math.Round(ahpResult.LambdaMax, 4),
                    CI = (decimal)Math.Round(ahpResult.CI, 4),
                    CR = (decimal)Math.Round(ahpResult.CR, 4)
                });
            }

            ky.TrangThai = "dat_trong_so";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Tính toán AHP thành công! Ma trận đạt tính nhất quán (CR = {ahpResult.CR:0.0000} <= 0.10).";
            return RedirectToAction(nameof(KetQuaTrongSo), new { id = maKyDanhGia });
        }

        // GET: AHP/KetQuaTrongSo/5
        public async Task<IActionResult> KetQuaTrongSo(int? id)
        {
            if (id == null)
            {
                var latestKy = await _context.KyDanhGias.OrderByDescending(k => k.MaKyDanhGia).FirstOrDefaultAsync();
                if (latestKy == null)
                {
                    return RedirectToAction(nameof(CreateKy));
                }
                id = latestKy.MaKyDanhGia;
            }

            var ky = await _context.KyDanhGias.FindAsync(id);
            if (ky == null)
            {
                return NotFound();
            }

            var tieuChis = await _context.TieuChis.OrderBy(t => t.MaTieuChi).ToListAsync();
            int n = tieuChis.Count;

            var matrixCells = await _context.MaTranSoSanhs
                .Where(m => m.MaKyDanhGia == id)
                .ToListAsync();

            double[,] mat = new double[n, n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    var cell = matrixCells.FirstOrDefault(m => m.MaTieuChiDong == tieuChis[i].MaTieuChi && m.MaTieuChiCot == tieuChis[j].MaTieuChi);
                    mat[i, j] = cell?.GiaTri.HasValue == true ? (double)cell.GiaTri.Value : 1.0;
                }
            }

            var ahpResult = _ahpService.CalculateAHP(mat);
            var trongSos = await _context.TrongSoTieuChis
                .Include(t => t.TieuChi)
                .Where(t => t.MaKyDanhGia == id)
                .ToListAsync();

            double[][] matJagged = new double[n][];
            for (int i = 0; i < n; i++)
            {
                matJagged[i] = new double[n];
                for (int j = 0; j < n; j++)
                {
                    matJagged[i][j] = mat[i, j];
                }
            }

            var viewModel = new MaTranSoSanhInputModel
            {
                MaKyDanhGia = ky.MaKyDanhGia,
                TenKyDanhGia = ky.TenKyDanhGia ?? $"Kỳ #{ky.MaKyDanhGia}",
                DanhSachTieuChi = tieuChis,
                MaTran = matJagged,
                KetQuaAHP = ahpResult,
                TrongSos = trongSos
            };

            return View(viewModel);
        }

        // ==========================================
        // 3. CHẤM ĐIỂM NHÀ CUNG CẤP (Mục 34)
        // ==========================================

        // GET: AHP/ChamDiemNCC/5
        public async Task<IActionResult> ChamDiemNCC(int? id)
        {
            if (id == null)
            {
                var latestKy = await _context.KyDanhGias
                    .Where(k => k.TrongSoTieuChis.Any())
                    .OrderByDescending(k => k.MaKyDanhGia)
                    .FirstOrDefaultAsync();

                if (latestKy == null)
                {
                    latestKy = await _context.KyDanhGias.OrderByDescending(k => k.MaKyDanhGia).FirstOrDefaultAsync();
                }

                if (latestKy == null)
                {
                    return RedirectToAction(nameof(CreateKy));
                }
                id = latestKy.MaKyDanhGia;
            }

            var ky = await _context.KyDanhGias.FindAsync(id);
            if (ky == null)
            {
                return NotFound();
            }

            var trongSos = await _context.TrongSoTieuChis
                .Include(t => t.TieuChi)
                .Where(t => t.MaKyDanhGia == id)
                .ToListAsync();

            if (!trongSos.Any())
            {
                TempData["ErrorMessage"] = "Kỳ đánh giá này chưa có trọng số tiêu chí. Vui lòng hoàn thành tính AHP trước.";
                return RedirectToAction(nameof(MaTranSoSanh), new { id });
            }

            var nccs = await _context.NhaCungCaps
                .Where(n => n.TrangThai == true)
                .OrderBy(n => n.MaNhaCungCap)
                .ToListAsync();

            var tieuChis = await _context.TieuChis.OrderBy(t => t.MaTieuChi).ToListAsync();

            // Lấy điểm đã chấm trước đó nếu có
            var existingScores = await _context.DiemDanhGiaNCCs
                .Where(d => d.MaKyDanhGia == id)
                .ToListAsync();

            var diemMap = new Dictionary<int, Dictionary<int, decimal>>();
            foreach (var ncc in nccs)
            {
                diemMap[ncc.MaNhaCungCap] = new Dictionary<int, decimal>();
                foreach (var tc in tieuChis)
                {
                    var sc = existingScores.FirstOrDefault(d => d.MaNhaCungCap == ncc.MaNhaCungCap && d.MaTieuChi == tc.MaTieuChi);
                    diemMap[ncc.MaNhaCungCap][tc.MaTieuChi] = sc?.Diem ?? 5.0m; // Mặc định điểm trung bình 5
                }
            }

            var viewModel = new ChamDiemNCCInputModel
            {
                MaKyDanhGia = ky.MaKyDanhGia,
                TenKyDanhGia = ky.TenKyDanhGia ?? $"Kỳ #{ky.MaKyDanhGia}",
                DanhSachTieuChi = tieuChis,
                TrongSos = trongSos,
                DanhSachNCC = nccs,
                DiemMap = diemMap
            };

            return View(viewModel);
        }

        // POST: AHP/LuuChamDiemVaXepHang
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> LuuChamDiemVaXepHang(int maKyDanhGia, Dictionary<int, Dictionary<int, decimal>> scores)
        {
            var ky = await _context.KyDanhGias.FindAsync(maKyDanhGia);
            if (ky == null)
            {
                TempData["ErrorMessage"] = "Kỳ đánh giá không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            var trongSos = await _context.TrongSoTieuChis
                .Where(t => t.MaKyDanhGia == maKyDanhGia)
                .OrderBy(t => t.MaTieuChi)
                .ToListAsync();

            if (!trongSos.Any())
            {
                TempData["ErrorMessage"] = "Chưa có trọng số AHP cho kỳ đánh giá này.";
                return RedirectToAction(nameof(MaTranSoSanh), new { id = maKyDanhGia });
            }

            var nccs = await _context.NhaCungCaps.Where(n => n.TrangThai == true).ToListAsync();
            var tieuChis = await _context.TieuChis.OrderBy(t => t.MaTieuChi).ToListAsync();

            // 1. Lưu điểm vào bảng DiemDanhGiaNCC
            var oldScores = await _context.DiemDanhGiaNCCs.Where(d => d.MaKyDanhGia == maKyDanhGia).ToListAsync();
            if (oldScores.Any())
            {
                _context.DiemDanhGiaNCCs.RemoveRange(oldScores);
            }

            foreach (var ncc in nccs)
            {
                foreach (var tc in tieuChis)
                {
                    decimal diemValue = 5.0m;
                    string formKey = $"scores[{ncc.MaNhaCungCap}][{tc.MaTieuChi}]";
                    string? rawVal = Request.Form[formKey].FirstOrDefault();
                    if (!string.IsNullOrEmpty(rawVal) && decimal.TryParse(rawVal.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedVal))
                    {
                        diemValue = parsedVal;
                    }
                    else if (scores != null && scores.ContainsKey(ncc.MaNhaCungCap) && scores[ncc.MaNhaCungCap].ContainsKey(tc.MaTieuChi))
                    {
                        diemValue = scores[ncc.MaNhaCungCap][tc.MaTieuChi];
                    }

                    // Giới hạn thang 1 - 9
                    diemValue = Math.Clamp(diemValue, 1.0m, 9.0m);

                    _context.DiemDanhGiaNCCs.Add(new DiemDanhGiaNCC
                    {
                        MaKyDanhGia = maKyDanhGia,
                        MaNhaCungCap = ncc.MaNhaCungCap,
                        MaTieuChi = tc.MaTieuChi,
                        Diem = diemValue
                    });
                }
            }

            // 2. Tính toán điểm tổng hợp Weighted Sum (WSM) (Mục 35) & Xếp hạng (Mục 36)
            var oldRankings = await _context.KetQuaXepHangs.Where(k => k.MaKyDanhGia == maKyDanhGia).ToListAsync();
            if (oldRankings.Any())
            {
                _context.KetQuaXepHangs.RemoveRange(oldRankings);
            }

            var supplierResults = new List<(int MaNCC, double DiemTong)>();
            double[] weights = trongSos.Select(t => (double)(t.TrongSo ?? 0)).ToArray();

            foreach (var ncc in nccs)
            {
                double[] nccCriteriaScores = new double[tieuChis.Count];
                for (int i = 0; i < tieuChis.Count; i++)
                {
                    int tcId = tieuChis[i].MaTieuChi;
                    decimal d = (scores != null && scores.ContainsKey(ncc.MaNhaCungCap) && scores[ncc.MaNhaCungCap].ContainsKey(tcId))
                        ? scores[ncc.MaNhaCungCap][tcId] : 5.0m;
                    nccCriteriaScores[i] = (double)d;
                }

                double totalScore = _ahpService.TinhDiemWeightedSum(weights, nccCriteriaScores);
                supplierResults.Add((ncc.MaNhaCungCap, totalScore));
            }

            // Sắp xếp điểm giảm dần
            var sortedList = supplierResults.OrderByDescending(r => r.DiemTong).ToList();

            // 3. Gán Xếp hạng và Đề xuất (DeXuat = 1 cho Hạng 1) (Mục 37)
            for (int rank = 1; rank <= sortedList.Count; rank++)
            {
                var item = sortedList[rank - 1];
                _context.KetQuaXepHangs.Add(new KetQuaXepHang
                {
                    MaKyDanhGia = maKyDanhGia,
                    MaNhaCungCap = item.MaNCC,
                    DiemTong = (decimal)Math.Round(item.DiemTong, 2),
                    XepHang = rank,
                    DeXuat = (rank == 1) // Hạng 1 là DeXuat = true
                });
            }

            ky.TrangThai = "hoanthanh";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã tính điểm tổng hợp và xếp hạng nhà cung cấp thành công!";
            return RedirectToAction(nameof(KetQuaXepHang), new { id = maKyDanhGia });
        }

        // ==========================================
        // 4. KẾT QUẢ XẾP HẠNG & ĐỀ XUẤT NCC (Mục 36, 37)
        // ==========================================

        // GET: AHP/KetQuaXepHang/5
        public async Task<IActionResult> KetQuaXepHang(int? id)
        {
            if (id == null)
            {
                var latestKy = await _context.KyDanhGias
                    .Where(k => k.KetQuaXepHangs.Any())
                    .OrderByDescending(k => k.MaKyDanhGia)
                    .FirstOrDefaultAsync();

                if (latestKy == null)
                {
                    TempData["ErrorMessage"] = "Chưa có kỳ đánh giá nào có kết quả xếp hạng.";
                    return RedirectToAction(nameof(Index));
                }
                id = latestKy.MaKyDanhGia;
            }

            var ky = await _context.KyDanhGias.FindAsync(id);
            if (ky == null)
            {
                return NotFound();
            }

            var tieuChis = await _context.TieuChis.OrderBy(t => t.MaTieuChi).ToListAsync();
            var trongSos = await _context.TrongSoTieuChis
                .Include(t => t.TieuChi)
                .Where(t => t.MaKyDanhGia == id)
                .ToListAsync();

            var rankings = await _context.KetQuaXepHangs
                .Include(k => k.NhaCungCap)
                .Where(k => k.MaKyDanhGia == id)
                .OrderBy(k => k.XepHang)
                .ToListAsync();

            var scores = await _context.DiemDanhGiaNCCs
                .Where(d => d.MaKyDanhGia == id)
                .ToListAsync();

            var diemMap = new Dictionary<int, Dictionary<int, decimal>>();
            foreach (var r in rankings)
            {
                if (r.MaNhaCungCap.HasValue)
                {
                    int nccId = r.MaNhaCungCap.Value;
                    diemMap[nccId] = new Dictionary<int, decimal>();
                    foreach (var tc in tieuChis)
                    {
                        var sc = scores.FirstOrDefault(d => d.MaNhaCungCap == nccId && d.MaTieuChi == tc.MaTieuChi);
                        diemMap[nccId][tc.MaTieuChi] = sc?.Diem ?? 0;
                    }
                }
            }

            var deXuat = rankings.FirstOrDefault(r => r.DeXuat == true) ?? rankings.FirstOrDefault();

            var viewModel = new KetQuaXepHangViewModel
            {
                MaKyDanhGia = ky.MaKyDanhGia,
                TenKyDanhGia = ky.TenKyDanhGia ?? $"Kỳ #{ky.MaKyDanhGia}",
                DanhSachTieuChi = tieuChis,
                TrongSos = trongSos,
                BangXepHang = rankings,
                DiemMap = diemMap,
                NhaCungCapDeXuat = deXuat
            };

            return View(viewModel);
        }

        // POST: AHP/DeleteKy/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,quanly")]
        public async Task<IActionResult> DeleteKy(int id)
        {
            var ky = await _context.KyDanhGias
                .Include(k => k.MaTranSoSanhs)
                .Include(k => k.TrongSoTieuChis)
                .Include(k => k.DiemDanhGiaNCCs)
                .Include(k => k.KetQuaXepHangs)
                .FirstOrDefaultAsync(k => k.MaKyDanhGia == id);

            if (ky == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy kỳ đánh giá cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            if (ky.MaTranSoSanhs.Any()) _context.MaTranSoSanhs.RemoveRange(ky.MaTranSoSanhs);
            if (ky.TrongSoTieuChis.Any()) _context.TrongSoTieuChis.RemoveRange(ky.TrongSoTieuChis);
            if (ky.DiemDanhGiaNCCs.Any()) _context.DiemDanhGiaNCCs.RemoveRange(ky.DiemDanhGiaNCCs);
            if (ky.KetQuaXepHangs.Any()) _context.KetQuaXepHangs.RemoveRange(ky.KetQuaXepHangs);

            _context.KyDanhGias.Remove(ky);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa kỳ đánh giá '{ky.TenKyDanhGia}' thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
