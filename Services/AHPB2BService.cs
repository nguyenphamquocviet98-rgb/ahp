using System;
using System.Collections.Generic;
using System.Linq;
using code.Models;

namespace code.Services
{
    public class AHPB2BCandidate
    {
        public int MaDoanhNghiep { get; set; }
        public string TenDoanhNghiep { get; set; } = string.Empty;
        public bool LaGianHangMall { get; set; }
        public decimal DiemUyTinGoc { get; set; }

        // Giá trị thực tế trên sàn
        public decimal GiaSi { get; set; }
        public int ThoiGianGiaoHang { get; set; }
        public int SoLuongTon { get; set; }

        // Điểm quy đổi thang Saaty 1-9
        public double DiemGia { get; set; }
        public double DiemGiaoHang { get; set; }
        public double DiemUyTin { get; set; }
        public double DiemTonKho { get; set; }

        // Điểm tổng hợp và thứ hạng
        public double DiemTongHop { get; set; }
        public int XepHang { get; set; }
        public bool DeXuat { get; set; }
    }

    public class AHPB2BEvaluationResult
    {
        public string TenSanPham { get; set; } = string.Empty;
        public int SoLuongCanMua { get; set; }
        public string[] TieuChis { get; set; } = { "Giá sỉ", "Thời gian giao", "Độ uy tín & Mall", "Năng lực tồn kho" };
        public double[,] MaTranSoSanh { get; set; } = new double[4, 4];
        public AHPResult AhpResult { get; set; } = new AHPResult();
        public List<AHPB2BCandidate> Candidates { get; set; } = new List<AHPB2BCandidate>();
        public AHPB2BCandidate? TopCandidate => Candidates.FirstOrDefault(c => c.DeXuat);
    }

    public class AHPB2BService
    {
        private readonly AHPService _ahpService;

        public AHPB2BService(AHPService ahpService)
        {
            _ahpService = ahpService;
        }

        /// <summary>
        /// Ma trận cặp mặc định cân bằng chuẩn:
        /// Tiêu chí: [0] Giá sỉ, [1] Thời gian giao, [2] Uy tín & Mall, [3] Tồn kho sẵn
        /// </summary>
        public double[,] GetDefaultMatrix(string scenario = "can_bang")
        {
            return scenario switch
            {
                // Ưu tiên giá rẻ nhất (Chi phí là quan trọng nhất)
                "gia_re" => new double[4, 4]
                {
                    { 1.0, 3.0, 4.0, 5.0 },
                    { 1.0/3.0, 1.0, 2.0, 3.0 },
                    { 1.0/4.0, 1.0/2.0, 1.0, 2.0 },
                    { 1.0/5.0, 1.0/3.0, 1.0/2.0, 1.0 }
                },

                // Ưu tiên giao hàng siêu tốc (Cần hàng gấp)
                "giao_nhanh" => new double[4, 4]
                {
                    { 1.0, 1.0/3.0, 2.0, 3.0 },
                    { 3.0, 1.0, 4.0, 5.0 },
                    { 1.0/2.0, 1.0/4.0, 1.0, 2.0 },
                    { 1.0/3.0, 1.0/5.0, 1.0/2.0, 1.0 }
                },

                // Ưu tiên Mall & Đối tác uy tín cao (Hàng chất lượng, thương hiệu lớn)
                "uy_tin" => new double[4, 4]
                {
                    { 1.0, 2.0, 1.0/3.0, 3.0 },
                    { 1.0/2.0, 1.0, 1.0/4.0, 2.0 },
                    { 3.0, 4.0, 1.0, 5.0 },
                    { 1.0/3.0, 1.0/2.0, 1.0/5.0, 1.0 }
                },

                // Cân bằng đa mục tiêu (Mặc định)
                _ => new double[4, 4]
                {
                    { 1.0, 2.0, 3.0, 4.0 },
                    { 1.0/2.0, 1.0, 2.0, 3.0 },
                    { 1.0/3.0, 1.0/2.0, 1.0, 2.0 },
                    { 1.0/4.0, 1.0/3.0, 1.0/2.0, 1.0 }
                }
            };
        }

        /// <summary>
        /// Thực hiện phân tích và xếp hạng AHP cho danh sách NPP chào bán mặt hàng
        /// </summary>
        public AHPB2BEvaluationResult Evaluate(List<TonKhoNPP> candidateKho, double[,] matrix, string tenSanPham, int soLuongCanMua)
        {
            if (soLuongCanMua <= 0) throw new ArgumentException("Số lượng cần mua phải lớn hơn 0.");
            if (matrix.GetLength(0) != 4 || matrix.GetLength(1) != 4)
                throw new ArgumentException("Bài toán sử dụng đúng 4 tiêu chí.");
            var res = new AHPB2BEvaluationResult
            {
                TenSanPham = tenSanPham,
                SoLuongCanMua = soLuongCanMua,
                MaTranSoSanh = matrix,
                AhpResult = _ahpService.CalculateAHP(matrix)
            };

            if (!res.AhpResult.IsConsistent) return res;
            candidateKho = candidateKho.Where(c => c.DangChaoBan && c.SoLuongTon >= soLuongCanMua &&
                c.GiaSi > 0 && c.ThoiGianGiaoHang > 0 && c.DoanhNghiep?.TrangThai == true).ToList();
            if (!candidateKho.Any())
            {
                return res;
            }

            // 1. Thu thập dữ liệu min/max để chuẩn hóa thang điểm 1-9
            double minGia = (double)candidateKho.Min(c => c.GiaSi);
            double maxGia = (double)candidateKho.Max(c => c.GiaSi);

            double minTime = candidateKho.Min(c => c.ThoiGianGiaoHang);
            double maxTime = candidateKho.Max(c => c.ThoiGianGiaoHang);

            double minTon = candidateKho.Min(c => c.SoLuongTon);
            double maxTon = candidateKho.Max(c => c.SoLuongTon);

            var list = new List<AHPB2BCandidate>();

            foreach (var item in candidateKho)
            {
                var dn = item.DoanhNghiep;
                var cand = new AHPB2BCandidate
                {
                    MaDoanhNghiep = item.MaDoanhNghiep,
                    TenDoanhNghiep = dn?.TenDoanhNghiep ?? ("NPP #" + item.MaDoanhNghiep),
                    LaGianHangMall = dn?.LaGianHangMall ?? false,
                    DiemUyTinGoc = dn?.DiemUyTin ?? 5.0m,
                    GiaSi = item.GiaSi,
                    ThoiGianGiaoHang = item.ThoiGianGiaoHang,
                    SoLuongTon = item.SoLuongTon
                };

                // Chuẩn hóa Tiêu chí 1: Giá sỉ (Cost: giá càng thấp điểm càng cao 1-9)
                if (maxGia == minGia)
                    cand.DiemGia = 9.0;
                else
                    cand.DiemGia = Math.Round(1.0 + 8.0 * (maxGia - (double)cand.GiaSi) / (maxGia - minGia), 2);

                // Chuẩn hóa Tiêu chí 2: Thời gian giao hàng (Cost: số ngày càng ngắn điểm càng cao 1-9)
                if (maxTime == minTime)
                    cand.DiemGiaoHang = 9.0;
                else
                    cand.DiemGiaoHang = Math.Round(1.0 + 8.0 * (maxTime - cand.ThoiGianGiaoHang) / (maxTime - minTime), 2);

                // Chuẩn hóa Tiêu chí 3: Độ uy tín & Mall (Benefit: càng cao điểm càng cao 1-9)
                // Gian hàng Mall được cộng thưởng 1.0 điểm uy tín
                double uyTinEff = Math.Clamp((double)cand.DiemUyTinGoc, 1.0, 10.0);
                cand.DiemUyTin = Math.Round(1.0 + 8.0 * (uyTinEff - 1.0) / 9.0, 2);

                // Chuẩn hóa Tiêu chí 4: Năng lực tồn kho sẵn (Benefit: tồn càng nhiều điểm càng cao 1-9)
                if (maxTon == minTon)
                    cand.DiemTonKho = 9.0;
                else
                    cand.DiemTonKho = Math.Round(1.0 + 8.0 * (cand.SoLuongTon - minTon) / (maxTon - minTon), 2);

                // Điểm tổng hợp theo trọng số AHP (Weighted Sum Model)
                double[] candidateScores = { cand.DiemGia, cand.DiemGiaoHang, cand.DiemUyTin, cand.DiemTonKho };
                cand.DiemTongHop = _ahpService.TinhDiemWeightedSum(res.AhpResult.TrongSo, candidateScores);

                list.Add(cand);
            }

            // Xếp hạng từ cao xuống thấp
            list = list.OrderByDescending(c => c.DiemTongHop).ThenBy(c => c.GiaSi)
                .ThenBy(c => c.ThoiGianGiaoHang).ThenBy(c => c.MaDoanhNghiep).ToList();
            for (int i = 0; i < list.Count; i++)
            {
                list[i].XepHang = i + 1;
                list[i].DeXuat = (i == 0); // Top 1 được đề xuất
            }

            res.Candidates = list;
            return res;
        }
    }
}
