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
            result.IsConsistent = result.CR <= 0.1001; // Cho phép sai số làm tròn nhỏ

            return result;
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
            int n = Math.Min(weights.Length, criteriaScores.Length);
            double totalScore = 0;
            for (int i = 0; i < n; i++)
            {
                totalScore += weights[i] * criteriaScores[i];
            }
            return Math.Round(totalScore, 3);
        }
    }
}
