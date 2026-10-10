using System;
using System.Collections.Generic;
using System.Linq;

namespace code.Services
{
    public class AHPResult
    {
        public int NumberOfCriteria { get; set; }
        public double[] TongCot { get; set; } = Array.Empty<double>();
        public double[,] MaTranChuanHoa { get; set; } = new double[0, 0];
        public double[] TrongSo { get; set; } = Array.Empty<double>();
        public double LambdaMax { get; set; }
        public double CI { get; set; }
        public double CR { get; set; }
        public bool IsConsistent { get; set; }
    }

    public class AHPService
    {
        // Bảng chỉ số ngẫu nhiên Saaty ngẫu nhiên (Random Index - RI)
        // Chỉ số tương ứng: n = 1 -> 10
        private static readonly double[] RandomIndex = { 0.0, 0.0, 0.58, 0.90, 1.12, 1.24, 1.32, 1.41, 1.45, 1.49 };

        /// <summary>
        /// Thực hiện toàn bộ quy trình tính toán AHP từ ma trận so sánh cặp ban đầu
        /// </summary>
        public AHPResult CalculateAHP(double[,] matrix)
        {
            ValidateMatrix(matrix);
            int n = matrix.GetLength(0);
            var result = new AHPResult { NumberOfCriteria = n };

            // Bước 1: Tính tổng từng cột
            result.TongCot = TinhTongCot(matrix);

            // Bước 2: Chuẩn hóa ma trận
            result.MaTranChuanHoa = ChuanHoaMaTran(matrix, result.TongCot);

            // Bước 3: Tính vector trọng số (trung bình từng hàng sau chuẩn hóa)
            result.TrongSo = TinhTrongSo(result.MaTranChuanHoa);

            // Bước 4: Tính Lambda Max
            result.LambdaMax = TinhLambdaMax(matrix, result.TrongSo);

            // Bước 5: Tính chỉ số nhất quán CI
            result.CI = TinhCI(result.LambdaMax, n);

            // Bước 6: Tính tỷ số nhất quán CR
            result.CR = TinhCR(result.CI, n);

            // Bước 7: Đánh giá tính nhất quán (CR <= 0.10)
            result.IsConsistent = double.IsFinite(result.CR) && result.CR <= 0.10 + 1e-12;

            return result;
        }

        public static void ValidateMatrix(double[,] matrix)
        {
            int n = matrix.GetLength(0);
            if (n < 1 || n > 10 || matrix.GetLength(1) != n)
                throw new ArgumentException("Ma trận phải vuông, từ 1 đến 10 tiêu chí.");
            for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                double value = matrix[i, j];
                if (!double.IsFinite(value) || value < 1.0 / 9 - 1e-6 || value > 9)
                    throw new ArgumentException("Giá trị so sánh phải nằm trong khoảng 1/9 đến 9.");
                if (i == j && Math.Abs(value - 1) > 1e-6)
                    throw new ArgumentException("Đường chéo ma trận phải bằng 1.");
                if (Math.Abs(value * matrix[j, i] - 1) > 1e-4)
                    throw new ArgumentException("Hai ô so sánh đối xứng phải nghịch đảo nhau.");
            }
        }

        // Bảng ma trận phiên bản cũ lưu 3 chữ số thập phân (1/9 thành 0.111).
        public static double RestoreStoredComparison(double value)
        {
            if (value > 0 && value < 1)
            {
                double divisor = Math.Round(1 / value);
                if (divisor >= 2 && divisor <= 9 && Math.Abs(value - 1 / divisor) <= 0.00051)
                    return 1 / divisor;
            }
            return value;
        }

        /// <summary>
        /// Bước 1: Tính tổng giá trị theo từng cột
        /// </summary>
        public double[] TinhTongCot(double[,] matrix)
        {
            int n = matrix.GetLength(0);
            double[] tongCot = new double[n];

            for (int j = 0; j < n; j++)
            {
                double sum = 0;
                for (int i = 0; i < n; i++)
                {
                    sum += matrix[i, j];
                }
                tongCot[j] = sum;
            }
            return tongCot;
        }

        /// <summary>
        /// Bước 2: Chuẩn hóa ma trận: Giá trị chuẩn hóa = Giá trị ô / Tổng cột
        /// </summary>
        public double[,] ChuanHoaMaTran(double[,] matrix, double[] tongCot)
        {
            int n = matrix.GetLength(0);
            double[,] normalized = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    normalized[i, j] = tongCot[j] > 0 ? matrix[i, j] / tongCot[j] : 0;
                }
            }
            return normalized;
        }

        /// <summary>
        /// Bước 3: Tính trọng số: Trung bình cộng các phần tử trên từng hàng của ma trận chuẩn hóa
        /// </summary>
        public double[] TinhTrongSo(double[,] normalizedMatrix)
        {
            int n = normalizedMatrix.GetLength(0);
            double[] weights = new double[n];

            for (int i = 0; i < n; i++)
            {
                double rowSum = 0;
                for (int j = 0; j < n; j++)
                {
                    rowSum += normalizedMatrix[i, j];
                }
                weights[i] = rowSum / n;
            }
            return weights;
        }

        /// <summary>
        /// Bước 4: Tính Lambda Max = Trung bình ((A x W)_i / W_i)
        /// </summary>
        public double TinhLambdaMax(double[,] matrix, double[] weights)
        {
            int n = matrix.GetLength(0);
            double[] aw = new double[n];

            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int j = 0; j < n; j++)
                {
                    sum += matrix[i, j] * weights[j];
                }
                aw[i] = sum;
            }

            double lambdaSum = 0;
            for (int i = 0; i < n; i++)
            {
                if (weights[i] > 0)
                {
                    lambdaSum += aw[i] / weights[i];
                }
            }
            return lambdaSum / n;
        }

        /// <summary>
        /// Bước 5: Tính chỉ số nhất quán CI = (LambdaMax - n) / (n - 1)
        /// </summary>
        public double TinhCI(double lambdaMax, int n)
        {
            if (n <= 1) return 0;
            return (lambdaMax - n) / (n - 1);
        }

        /// <summary>
        /// Bước 6: Tính tỷ số nhất quán CR = CI / RI
        /// </summary>
        public double TinhCR(double ci, int n)
        {
            if (n <= 2) return 0;
            double ri = (n <= RandomIndex.Length) ? RandomIndex[n - 1] : 1.49;
            if (ri == 0) return 0;
            return ci / ri;
        }

        /// <summary>
        /// Tính điểm tổng hợp theo Weighted Sum Model: Score = Sum(W_i * Diem_i)
        /// </summary>
        public double TinhDiemWeightedSum(double[] weights, double[] criteriaScores)
        {
            if (weights.Length == 0 || weights.Length != criteriaScores.Length ||
                weights.Any(w => !double.IsFinite(w) || w < 0) ||
                criteriaScores.Any(s => !double.IsFinite(s) || s < 1 || s > 9))
                throw new ArgumentException("Trọng số và điểm phải đủ, hữu hạn; điểm thuộc thang 1–9.");
            int n = weights.Length;
            double totalScore = 0;
            for (int i = 0; i < n; i++)
            {
                totalScore += weights[i] * criteriaScores[i];
            }
            return Math.Round(totalScore, 3);
        }
    }
}
