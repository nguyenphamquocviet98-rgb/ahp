using System.Text;
using System.Text.Json;
using code.Models;

namespace code.Services
{
    public class AiAdvisorResult
    {
        public bool Success { get; set; } = true;
        public string EngineUsed { get; set; } = "Local Rule-Based Expert Engine";
        public string TomTatDeXuat { get; set; } = "";
        public List<string> LyDoChon { get; set; } = new();
        public List<string> CanhBaoRuiRo { get; set; } = new();
        public List<string> ChienLuocDamPhan { get; set; } = new();
        public string KhuyenNghiHanhDong { get; set; } = "";
        public string FullAnalysisText { get; set; } = "";
    }

    public class AiAdvisorService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger<AiAdvisorService> _logger;

        public AiAdvisorService(IConfiguration config, HttpClient httpClient, ILogger<AiAdvisorService> logger)
        {
            _config = config;
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<AiAdvisorResult> AnalyzeAhpDecisionAsync(PhienAHP_B2B phien, Dictionary<int, TonKhoNPP> khoDict)
        {
            string apiKey = _config["Gemini:ApiKey"] ?? "";
            string modelName = _config["Gemini:Model"] ?? "gemini-1.5-flash";

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                try
                {
                    var geminiResult = await CallGeminiApiAsync(apiKey, modelName, phien, khoDict);
                    if (geminiResult != null && geminiResult.Success)
                    {
                        return geminiResult;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Gọi Gemini API thất bại, tự động chuyển sang Local Expert Engine.");
                }
            }

            // Fallback: Local Rule-Based Expert Engine
            return GenerateLocalExpertAnalysis(phien, khoDict);
        }

        /// <summary>
        /// Local Rule-Based Expert Engine: Phân tích ma trận kết quả AHP, giá sỉ, giao hàng, tồn kho & rủi ro chuỗi cung ứng
        /// </summary>
        public AiAdvisorResult GenerateLocalExpertAnalysis(PhienAHP_B2B phien, Dictionary<int, TonKhoNPP> khoDict)
        {
            var result = new AiAdvisorResult
            {
                Success = true,
                EngineUsed = "Giải thích theo quy tắc (không dùng mô hình AI)"
            };

            var rankedList = phien.KetQuaAHPs?.OrderBy(k => k.XepHang).ToList() ?? new List<KetQuaAHP_B2B>();
            if (!rankedList.Any())
            {
                result.TomTatDeXuat = "Chưa có dữ liệu đánh giá xếp hạng ứng viên trong phiên AHP này.";
                return result;
            }

            var top1 = rankedList.First();
            var top2 = rankedList.Skip(1).FirstOrDefault();
            var sp = phien.SanPham;
            string tenSp = sp?.TenSanPham ?? "mặt hàng";
            int soLuong = phien.SoLuongCanMua;

            TonKhoNPP? top1Kho = khoDict.ContainsKey(top1.MaDoanhNghiep) ? khoDict[top1.MaDoanhNghiep] : null;
            TonKhoNPP? top2Kho = (top2 != null && khoDict.ContainsKey(top2.MaDoanhNghiep)) ? khoDict[top2.MaDoanhNghiep] : null;

            // Tìm đối tác rẻ nhất, giao nhanh nhất, uy tín nhất
            var cheapest = khoDict.Values.OrderBy(k => k.GiaSi).FirstOrDefault();
            var fastest = khoDict.Values.OrderBy(k => k.ThoiGianGiaoHang).FirstOrDefault();

            // 1. Tóm tắt đề xuất
            double diemTop1 = (double)top1.DiemTongHop;
            double diemTop2 = top2 != null ? (double)top2.DiemTongHop : 0;
            double chenhLechDiem = top2 != null ? (diemTop1 - diemTop2) : 0;

            string mallBadge = (top1.DoanhNghiep?.LaGianHangMall == true) ? " (Gian hàng Chính Hãng Mall)" : "";
            result.TomTatDeXuat = $"Dựa trên trọng số AHP và tổng điểm WSM của 4 tiêu chí, **{top1.DoanhNghiep?.TenDoanhNghiep}**{mallBadge} là đối tác cung ứng tối ưu nhất toàn diện với điểm số **{diemTop1:F3}/9.000**, vượt trội so với vị trí thứ 2 ({chenhLechDiem:F3} điểm).";

            // 2. Lý do lựa chọn cốt lõi
            if (top1Kho != null)
            {
                if (cheapest != null && top1Kho.GiaSi <= cheapest.GiaSi)
                {
                    result.LyDoChon.Add($"💰 **Giá sỉ tốt nhất sàn:** Chào giá {top1Kho.GiaSi:N0} đ/sp, tối ưu hóa tối đa chi phí vốn lưu động cho đơn hàng {soLuong:N0} sản phẩm.");
                }
                else if (cheapest != null)
                {
                    decimal chenhGia = top1Kho.GiaSi - cheapest.GiaSi;
                    result.LyDoChon.Add($"⚖️ **Chi phí hợp lý so với chất lượng dịch vụ:** Chào giá {top1Kho.GiaSi:N0} đ/sp (chỉ chênh lệch nhẹ {chenhGia:N0} đ/sp so với bên rẻ nhất nhưng vượt trội về uy tín và thời gian giao hàng).");
                }

                if (fastest != null && top1Kho.ThoiGianGiaoHang <= fastest.ThoiGianGiaoHang)
                {
                    result.LyDoChon.Add($"⚡ **Tốc độ giao vận thần tốc:** Cam kết chỉ {top1Kho.ThoiGianGiaoHang} ngày, giúp doanh nghiệp quay vòng vốn và bổ sung hàng vào kho nhanh nhất.");
                }
                else
                {
                    result.LyDoChon.Add($"🚚 **Thời gian giao hàng ổn định:** {top1Kho.ThoiGianGiaoHang} ngày đáp ứng tốt kế hoạch nhập hàng định kỳ.");
                }

                if (top1.DoanhNghiep?.LaGianHangMall == true)
                {
                    result.LyDoChon.Add($"🛡️ **Bảo chứng Gian hàng Mall:** Đạt chuẩn gian hàng chính hãng được sàn B2B xác thực năng lực pháp lý và chất lượng nguồn gốc sản phẩm.");
                }

                result.LyDoChon.Add($"⭐ **Uy tín được cộng đồng B2B kiểm chứng:** Điểm đánh giá đạt {top1.DoanhNghiep?.DiemUyTin:F1}/10 sao qua các đơn hàng giao dịch thành công trước đó.");

                if (top1Kho.SoLuongTon >= soLuong * 2)
                {
                    result.LyDoChon.Add($"📦 **Năng lực cung ứng dồi dào:** Tồn kho sẵn {top1Kho.SoLuongTon:N0} sp, vượt {((double)top1Kho.SoLuongTon / soLuong):F1} lần nhu cầu hiện tại, loại bỏ nguy cơ đứt gãy nguồn hàng.");
                }
            }

            // 3. Cảnh báo rủi ro tiềm ẩn (Risk Assessment)
            if (top1Kho != null)
            {
                if (top1Kho.SoLuongTon < soLuong * 1.2)
                {
                    result.CanhBaoRuiRo.Add($"⚠️ **Rủi ro cạn kho:** Tồn kho khả dụng ({top1Kho.SoLuongTon:N0}) sát với nhu cầu đặt ({soLuong:N0}). Nên xác nhận giữ hàng hoặc chốt đơn ngay trước khi đối tác khác đặt mất.");
                }

                if (cheapest != null && top1Kho.GiaSi > cheapest.GiaSi)
                {
                    decimal tongTienChenh = (top1Kho.GiaSi - cheapest.GiaSi) * soLuong;
                    result.CanhBaoRuiRo.Add($"💸 **Chênh lệch chi phí vốn:** So với mức giá thấp nhất trên sàn của {cheapest.DoanhNghiep?.TenDoanhNghiep}, bạn đang chi trả cao hơn khoảng {tongTienChenh:N0} đ để đổi lấy thời gian giao hàng và uy tín cao hơn.");
                }

                if (top2 != null && chenhLechDiem < 0.25)
                {
                    result.CanhBaoRuiRo.Add($"⚖️ **Khoảng cách điểm số sít sao:** Vị trí #1 và #2 cách nhau chỉ {chenhLechDiem:F3} điểm. Nếu NPP #1 không đáp ứng được điều kiện thanh toán linh hoạt, NPP #{top2!.XepHang} ({top2.DoanhNghiep?.TenDoanhNghiep}) là phương án dự phòng hoàn hảo.");
                }
            }

            if (!result.CanhBaoRuiRo.Any())
            {
                result.CanhBaoRuiRo.Add("✅ **Chỉ số an toàn cao:** Chưa phát hiện rủi ro nghiêm trọng về tiến độ, giá cả hay năng lực cung ứng của đối tác này.");
            }

            // 4. Chiến lược đàm phán B2B (Negotiation Strategy)
            if (top1Kho != null)
            {
                if (top2Kho != null && top2Kho.GiaSi < top1Kho.GiaSi)
                {
                    decimal chietKhauKhaThi = top1Kho.GiaSi - top2Kho.GiaSi;
                    decimal tietKiemKyVong = chietKhauKhaThi * soLuong;
                    result.ChienLuocDamPhan.Add($"🎯 **Đòn bẩy giá đối thủ:** NPP #{top2!.XepHang} ({top2.DoanhNghiep?.TenDoanhNghiep}) đang chào giá {top2Kho.GiaSi:N0} đ/sp. Hãy dùng báo giá này để yêu cầu {top1.DoanhNghiep?.TenDoanhNghiep} giảm giá từ {chietKhauKhaThi:N0} đ/sp hoặc hỗ trợ chi phí vận chuyển, có thể tiết kiệm tới **{tietKiemKyVong:N0} đ**.");
                }
                else
                {
                    result.ChienLuocDamPhan.Add($"🎯 **Chiết khấu thanh toán nhanh:** Đối tác đã có giá chào tốt nhất sàn ({top1Kho.GiaSi:N0} đ). Hãy đề xuất thanh toán chuyển khoản trước 100% hoặc rút ngắn chu kỳ công nợ để yêu cầu chiết khấu thêm 1% - 2%.");
                }

                if (soLuong >= 50)
                {
                    result.ChienLuocDamPhan.Add($"🤝 **Cam kết hợp đồng khung dài hạn:** Với quy mô đơn hàng {soLuong:N0} sp, đề xuất ký hợp đồng nguyên tắc theo quý/năm để được cố định đơn giá và ưu tiên giữ tồn kho trong mùa cao điểm.");
                }

                result.ChienLuocDamPhan.Add($"📑 **Điều khoản phạt tiến độ:** Do thời gian giao hàng là {top1Kho.ThoiGianGiaoHang} ngày, nên đưa điều khoản phạt chậm giao hàng 0.5%/ngày vào hợp đồng B2B.");
            }

            // 5. Khuyến nghị hành động
            result.KhuyenNghiHanhDong = $"👉 **HÀNH ĐỘNG KHUYẾN NGHỊ:** Tiến hành bấm nút **[ĐẶT HÀNG NGAY]** gửi đơn B2B tới **{top1.DoanhNghiep?.TenDoanhNghiep}** với số lượng **{soLuong:N0} {sp?.DonViTinh?.TenDonVi}**. Trong phần ghi chú đơn hàng, đề xuất áp dụng chính sách chiết khấu sỉ theo tư vấn chiến lược ở trên.";

            return result;
        }

        /// <summary>
        /// Gọi Google Gemini API để tạo nhận định chuyên sâu bằng LLM
        /// </summary>
        private async Task<AiAdvisorResult?> CallGeminiApiAsync(string apiKey, string modelName, PhienAHP_B2B phien, Dictionary<int, TonKhoNPP> khoDict)
        {
            var rankedList = phien.KetQuaAHPs?.OrderBy(k => k.XepHang).ToList() ?? new List<KetQuaAHP_B2B>();
            if (!rankedList.Any()) return null;

            var sb = new StringBuilder();
            sb.AppendLine($"Sản phẩm cần mua: {phien.SanPham?.TenSanPham}");
            sb.AppendLine($"Số lượng cần mua: {phien.SoLuongCanMua} {phien.SanPham?.DonViTinh?.TenDonVi}");
            var snapshot = AhpSnapshot.Read(phien.SnapshotJson);
            if (snapshot != null)
                sb.AppendLine($"Trọng số (Giá, Giao hàng, Uy tín, Tồn kho): {string.Join(", ", snapshot.Weights)}; CR={snapshot.CR:F4}. Dữ liệu tại thời điểm đánh giá. Mall không cộng điểm.");
            sb.AppendLine("Kết quả chấm điểm AHP + WSM và dữ liệu chào hàng:");

            foreach (var r in rankedList)
            {
                var kho = khoDict.ContainsKey(r.MaDoanhNghiep) ? khoDict[r.MaDoanhNghiep] : null;
                string isMall = r.DoanhNghiep?.LaGianHangMall == true ? "[MALL]" : "[THƯỜNG]";
                sb.AppendLine($"- Top {r.XepHang}: {r.DoanhNghiep?.TenDoanhNghiep} {isMall}");
                sb.AppendLine($"  + Điểm AHP: {r.DiemTongHop:F3} (Giá: {r.DiemGia:F2}, Giao hàng: {r.DiemGiaoHang:F2}, Uy tín: {r.DiemUyTin:F2}, Tồn kho: {r.DiemTonKho:F2})");
                if (kho != null)
                {
                    sb.AppendLine($"  + Giá sỉ: {kho.GiaSi:N0} đ | Giao hàng: {kho.ThoiGianGiaoHang} ngày | Tồn kho: {kho.SoLuongTon} | Điểm uy tín sàn: {r.DoanhNghiep?.DiemUyTin}/10");
                }
            }

            string prompt = $@"Bạn là Giám đốc Thu mua & Chuyên gia Phân tích Chuỗi Cung ứng B2B cấp cao. 
Hãy phân tích báo cáo kết quả AHP sau đây và đưa ra tư vấn mua sỉ cho Giám đốc Doanh nghiệp:

{sb}

Hãy trả về DUY NHẤT một chuỗi JSON hợp lệ (không kèm markdown ```json hay văn bản thừa ngoài JSON) với cấu trúc sau:
{{
  ""TomTatDeXuat"": ""Tóm tắt ngắn gọn 1-2 câu lý do đối tác Top 1 được chọn"",
  ""LyDoChon"": [""Điểm mạnh 1"", ""Điểm mạnh 2"", ""Điểm mạnh 3""],
  ""CanhBaoRuiRo"": [""Rủi ro tiềm ẩn 1"", ""Rủi ro tiềm ẩn 2""],
  ""ChienLuocDamPhan"": [""Chiến lược đàm phán giá hoặc công nợ 1"", ""Chiến lược đàm phán 2""],
  ""KhuyenNghiHanhDong"": ""Hành động dứt khoát cần thực hiện ngay""
}}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.4,
                    maxOutputTokens = 1000
                }
            };

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Gemini API HTTP Error: {response.StatusCode}");
                return null;
            }

            string resJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(resJson);
            var candidatesElem = doc.RootElement.GetProperty("candidates");
            if (candidatesElem.GetArrayLength() == 0) return null;

            string aiText = candidatesElem[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            // Trích xuất JSON từ phản hồi nếu AI bọc trong ```json ... ```
            string cleanJson = aiText.Trim();
            if (cleanJson.StartsWith("```json"))
            {
                cleanJson = cleanJson.Substring(7);
            }
            if (cleanJson.StartsWith("```"))
            {
                cleanJson = cleanJson.Substring(3);
            }
            if (cleanJson.EndsWith("```"))
            {
                cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
            }
            cleanJson = cleanJson.Trim();

            var parsedResult = JsonSerializer.Deserialize<AiAdvisorResult>(cleanJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsedResult != null)
            {
                parsedResult.Success = true;
                parsedResult.EngineUsed = $"Google Gemini AI ({modelName})";
                return parsedResult;
            }

            return null;
        }
    }
}
