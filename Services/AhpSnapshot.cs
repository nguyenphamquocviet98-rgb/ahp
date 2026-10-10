using System.Text.Json;
using code.Models;

namespace code.Services;

// Lưu đầu vào tại thời điểm đánh giá; giá/kho thay đổi sau này không sửa báo cáo cũ.
public class AhpSnapshot
{
    public int Version { get; set; } = 1;
    public string Method { get; set; } = "AHP + WSM; min-max 1–9; uy tín 1–10; không cộng điểm Mall";
    public string ProductName { get; set; } = "";
    public double[][] Matrix { get; set; } = [];
    public double[] Weights { get; set; } = [];
    public double LambdaMax { get; set; }
    public double CI { get; set; }
    public double CR { get; set; }
    public List<AHPB2BCandidate> Candidates { get; set; } = [];

    public static AhpSnapshot From(AHPB2BEvaluationResult result) => new()
    {
        ProductName = result.TenSanPham,
        Matrix = Enumerable.Range(0, 4).Select(i => Enumerable.Range(0, 4)
            .Select(j => result.MaTranSoSanh[i, j]).ToArray()).ToArray(),
        Weights = result.AhpResult.TrongSo,
        LambdaMax = result.AhpResult.LambdaMax,
        CI = result.AhpResult.CI,
        CR = result.AhpResult.CR,
        Candidates = result.Candidates
    };

    public static AhpSnapshot? Read(string? json) => string.IsNullOrWhiteSpace(json)
        ? null : JsonSerializer.Deserialize<AhpSnapshot>(json);

    public Dictionary<int, TonKhoNPP> ToStock(int productId) => Candidates.ToDictionary(c => c.MaDoanhNghiep, c => new TonKhoNPP
    {
        MaDoanhNghiep = c.MaDoanhNghiep, MaSanPham = productId,
        GiaSi = c.GiaSi, SoLuongTon = c.SoLuongTon, ThoiGianGiaoHang = c.ThoiGianGiaoHang,
        DoanhNghiep = new DoanhNghiep { MaDoanhNghiep = c.MaDoanhNghiep, TenDoanhNghiep = c.TenDoanhNghiep,
            DiemUyTin = c.DiemUyTinGoc, LaGianHangMall = c.LaGianHangMall }
    });
}
