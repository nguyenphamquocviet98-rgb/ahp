# KỊCH BẢN BẢO VỆ ĐỒ ÁN VÀ VẤN ĐÁP HỘI ĐỒNG

Đề tài: **Quản lý nhập hàng và lựa chọn nhà cung cấp bằng AHP**  
Tham chiếu thiết kế: [`../DE_XUAT_NANG_CAP_DE_TAI_AHP.md`](../DE_XUAT_NANG_CAP_DE_TAI_AHP.md) & [`PHAM_VI_VA_KIEM_THU.md`](PHAM_VI_VA_KIEM_THU.md).

---

## 1. KỊCH BẢN TRÌNH DIỄN (DEMO) 10–12 PHÚT

| Thời gian | Bước thao tác | Mục tiêu chứng minh trước Hội đồng |
|---|---|---|
| **0 – 1 phút** | Giới thiệu bài toán: Doanh nghiệp cần nhập một sản phẩm cụ thể, số lượng lớn và thời hạn giao gấp. | Đặt rõ bối cảnh quyết định mua hàng thực tế; không phải nhập kho vu vơ. |
| **1 – 2 phút** | Phát hiện cảnh báo tồn thấp dưới ngưỡng an toàn $\rightarrow$ Lập yêu cầu mua hàng. | Chứng minh quy trình bắt đầu từ nhu cầu vận hành thực tế. |
| **2 – 4 phút** | Mở thiết lập AHP: Chọn 4 tiêu chí (Giá, Giao hàng, Uy tín, Tồn kho). Nhập thử ma trận so sánh mâu thuẫn để chứng minh hệ thống **chặn $CR > 0.10$**. Sau đó chuyển sang bộ so sánh hợp lệ ($CR \le 0.10$) để tính vector trọng số. | Nắm vững toán học AHP, hiểu rõ bản chất của tỷ số nhất quán và tính trung thực của thuật toán. |
| **4 – 6 phút** | Hệ thống lọc danh sách nhà cung cấp đủ điều kiện $\rightarrow$ Chuẩn hóa dữ liệu theo min-max $\rightarrow$ Tính điểm tổng hợp WSM $\rightarrow$ Trình bày bảng giải thích mức độ đóng góp của từng tiêu chí. | Minh chứng dữ liệu chấm điểm lấy từ giao dịch thực tế, kết quả có căn cứ khoa học rõ ràng, không thiên vị cảm tính. |
| **6 – 7 phút** | **Phân tích độ nhạy (Sensitivity Analysis):** Kéo thay đổi trọng số tiêu chí Chi phí để chỉ ra ngưỡng nào thì NCC B bị NCC A vượt lên dẫn đầu. | Thể hiện chiều sâu học thuật: Đánh giá độ bền vững của quyết định mua hàng. |
| **7 – 9 phút** | Quản lý xác nhận kết quả $\rightarrow$ Phát hành đơn đặt hàng $\rightarrow$ Bên bán duyệt xuất kho $\rightarrow$ Bên mua thực hiện nhận hàng và kiểm tra chất lượng (KCS: ghi nhận số lượng đạt và số lượng lỗi). | Quy trình kho khép kín, phân tách rõ hàng đạt chất lượng mới ghi tăng vào tồn bán được. |
| **9 – 10 phút** | Kiểm tra thẻ kho: Tồn kho tăng đúng lượng đạt. Mở lịch sử đánh giá: Tỷ lệ giao hàng đúng hạn và chất lượng của NCC vừa giao được tự động tích lũy cho các kỳ AHP sau. | Dữ liệu giao dịch tự động phản hồi ngược lại làm dữ liệu đầu vào cho các lần lựa chọn tiếp theo. |
| **10 – 12 phút** | Mở mã nguồn và chạy kiểm thử tự động (Unit Test / Regression Test) $\rightarrow$ Trả lời câu hỏi vấn đáp. | Độ tin cậy và tính hoàn thiện cao của phần mềm. |

---

## 2. BỘ CÂU HỎI VẤN ĐÁP TRỌNG ĐIỂM CỦA HỘI ĐỒNG & CÂU TRẢ LỜI CHUẨN

### Câu 1: Tại sao bài này lại dùng AHP? AHP được áp dụng ở khâu nào?
> **Trả lời:** AHP được áp dụng chính xác ở khâu **xác định trọng số ưu tiên của các tiêu chí mua hàng** thông qua ma trận so sánh cặp Saaty và kiểm soát tính nhất quán ($CR \le 0.10$). Sau khi có trọng số, hệ thống kết hợp với dữ liệu giao dịch thực tế (giá chào, tồn kho, lịch sử giao hàng) qua mô hình tổng hợp WSM để xếp hạng nhà cung cấp. Cách tiếp cận này khách quan hơn việc người quản lý tự gán trọng số theo cảm tính.

### Câu 2: Tỷ số nhất quán $CR \le 0.10$ có chứng minh nhà cung cấp được chọn là tốt nhất ngoài đời thực không?
> **Trả lời:** Dạ không. $CR$ chỉ đo lường **tính nhất quán logic trong các nhận định so sánh cặp của người ra quyết định** (tránh mâu thuẫn như $A > B$, $B > C$ mà lại nói $C > A$). Kết quả lựa chọn cuối cùng còn phụ thuộc vào tính xác thực của dữ liệu đầu vào và các tiêu chí được chọn.

### Câu 3: Tại sao không chọn nhà cung cấp có giá rẻ nhất?
> **Trả lời:** Chọn nhà cung cấp chỉ dựa vào giá rẻ tiềm ẩn rủi ro rất lớn: giao trễ hạn, tồn kho không đủ để giao hoặc tỷ lệ hàng lỗi cao gây gián đoạn kinh doanh. Hệ thống AHP của em phân tích đa tiêu chí để tìm ra phương án tối ưu tổng thể phù hợp với tình huống mua hàng (ví dụ khi cần hàng gấp thì ưu tiên giao nhanh, khi tồn kho dồi dào mới ưu tiên giá rẻ).

### Câu 4: Phân tích độ nhạy (Sensitivity Analysis) có ý nghĩa gì trong bài toán này?
> **Trả lời:** Giúp người quản lý biết được quyết định lựa chọn có "bền vững" hay không. Nếu chỉ cần trọng số giá tăng nhẹ 2% mà nhà cung cấp khác đã vượt lên thì quyết định rất nhạy cảm; ngược lại nếu trong khoảng biến thiên rộng từ 20% đến 60% nhà cung cấp đó vẫn dẫn đầu thì quyết định cực kỳ an toàn.

### Câu 5: Hệ thống đảm bảo tồn kho và số liệu không bị tính hai lần bằng cách nào?
> **Trả lời:** Em sử dụng giao dịch cơ sở dữ liệu (Transaction) kết hợp với cờ trạng thái và cơ chế Concurrency Token trong Entity Framework Core. Khi đơn hàng chuyển sang trạng thái "Đã nhận", trigger và logic máy chủ chỉ tăng kho đúng một lần và chặn hoàn toàn các thao tác gửi trùng lặp.
