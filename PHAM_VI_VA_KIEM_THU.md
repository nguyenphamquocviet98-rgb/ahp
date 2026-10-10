# QUẢN LÝ NHẬP HÀNG VÀ LỰA CHỌN NHÀ CUNG CẤP BẰNG AHP
## TÀI LIỆU ĐẶC TẢ PHẠM VI, THUẬT TOÁN VÀ KIỂM THỬ

**Tham chiếu cốt lõi:** `../DE_XUAT_NANG_CAP_DE_TAI_AHP.md` (Bản nâng cấp ngày 07/10/2026).

---

## 1. PHẠM VI HỆ THỐNG VÀ BỐI CẢNH ỨNG DỤNG

* **Tên đề tài:** Quản lý nhập hàng và lựa chọn nhà cung cấp bằng AHP.
* **Đối tượng trọng tâm:** Hoạt động mua và quản lý nhập hàng của doanh nghiệp; giải quyết triệt để bài toán lựa chọn nhà cung cấp tối ưu dựa trên dữ liệu đa tiêu chí và phương pháp AHP của Thomas L. Saaty.
* **Bối cảnh nguồn dữ liệu:** Nền tảng chào hàng từ các nhà phân phối đóng vai trò là môi trường tạo lập dữ liệu đầu vào thực tế (báo giá, tồn kho sẵn có, thời gian giao hàng cam kết, mức độ uy tín).

### Quy trình nghiệp vụ 2 luồng:
1. **Luồng mua có hỗ trợ AHP (Luồng chính):**
   - Xác định nhu cầu $\rightarrow$ Thiết lập ma trận so sánh cặp các tiêu chí $\rightarrow$ Kiểm tra tỷ số nhất quán $CR \le 0.10$ $\rightarrow$ Chuẩn hóa dữ liệu nhà cung cấp $\rightarrow$ Xếp hạng WSM $\rightarrow$ Đặt hàng $\rightarrow$ Bên bán duyệt xuất hàng $\rightarrow$ Bên mua nhận hàng & kiểm tra chất lượng $\rightarrow$ Tăng tồn kho và tích lũy dữ liệu hiệu suất.
2. **Luồng nhập kho truyền thống (Bổ trợ):**
   - Lập phiếu nhập trực tiếp từ nhà cung cấp ngoài luồng $\rightarrow$ Thêm chi tiết mặt hàng $\rightarrow$ Người quản lý kiểm tra và xác nhận $\rightarrow$ Trigger SQL Server cập nhật số dư kho và lưu nhật ký.

---

## 2. PHƯƠNG PHÁP TOÁN HỌC VÀ THUẬT TOÁN AHP

### 2.1. Bộ tiêu chí đánh giá
Hệ thống sử dụng bộ 4 tiêu chí cốt lõi:
1. **Chi phí mua (Cost - min):** Đơn giá sản phẩm và chi phí phân bổ.
2. **Thời gian giao hàng (Cost - min):** Số ngày cam kết giao hàng từ lúc đặt.
3. **Mức độ uy tín (Benefit - max):** Đánh giá lịch sử và mức độ pháp lý / chứng nhận.
4. **Khả năng đáp ứng / Tồn kho (Benefit - max):** Số lượng hàng sẵn sàng cung cấp.

### 2.2. Tính toán ma trận và kiểm tra tỷ số nhất quán ($CR$)
* Ma trận so sánh cặp $A = [a_{ij}]_{n \times n}$ thỏa mãn $a_{ii} = 1$ và $a_{ji} = 1/a_{ij}$ theo thang đo Saaty (1 đến 9).
* Tính vector trọng số $w$ bằng phương pháp chuẩn hóa cột và tính trung bình hàng:
  $$w_i = \frac{1}{n} \sum_{j=1}^{n} \frac{a_{ij}}{\sum_{k=1}^{n} a_{kj}}$$
* Xác định $\lambda_{max}$, Chỉ số nhất quán $CI$ và Tỷ số nhất quán $CR$:
  $$CI = \frac{\lambda_{max} - n}{n - 1}, \quad CR = \frac{CI}{RI}$$
  *(Với $n = 4$, chỉ số ngẫu nhiên chuẩn $RI = 0.90$)*.
* **Ràng buộc:** Nếu $CR > 0.10$, ma trận bị từ chối và cảnh báo người dùng điều chỉnh nhận định mâu thuẫn.

### 2.3. Quy đổi dữ liệu và Tổng hợp điểm (WSM)
* Chuẩn hóa dữ liệu theo hàm min-max về thang đo $[0, 1]$:
  - Tiêu chí chi phí: $u(x) = \frac{x_{max} - x}{x_{max} - x_{min}}$
  - Tiêu chí lợi ích: $u(x) = \frac{x - x_{min}}{x_{max} - x_{min}}$
* Điểm tổng hợp của ứng viên $i$:
  $$S_i = \sum_{j=1}^{n} w_j \times u_j(x_{ij})$$
* **Truy vết & Giải thích:** Mỗi phiên đánh giá lưu đầy đủ ma trận, trọng số và dữ liệu vào `PhienAHP_B2B.SnapshotJson` để có thể tái hiện và giải thích mức độ đóng góp của từng tiêu chí.

### 2.4. Phân tích độ nhạy (Sensitivity Analysis)
Cho phép khảo sát biến thiên của thứ hạng khi trọng số của tiêu chí $k$ thay đổi thành $t \in [0, 1]$:
$$w'_k = t, \quad w'_j = w_j \times \frac{1 - t}{1 - w_k} \quad (\forall j \ne k)$$
Giúp người quản lý xác định khoảng an toàn của quyết định và tìm ra ngưỡng mà tại đó nhà cung cấp khác sẽ vươn lên dẫn đầu.

---

## 3. CƠ CHẾ DỮ LIỆU VÀ TOÀN VẸN KHO

1. **Kiểm tra chất lượng và tăng tồn kho:**
   - Khi nhận hàng, chỉ số lượng đạt kiểm tra mới được ghi tăng vào tồn kho có thể bán.
   - Thao tác gửi lặp (resubmit) được chặn bằng cơ chế concurrency token và transaction.
2. **Bảo mật và Phân quyền:**
   - Mật khẩu người dùng được băm an toàn bằng `IPasswordHasher<T>`.
   - Kiểm tra quyền nghiêm ngặt tại Controller / Service phía máy chủ.
   - Chặn phân quyền chéo giữa các doanh nghiệp.

---

## 4. HƯỚNG DẪN KIỂM CHỨNG VÀ CHẠY HỆ THỐNG

### Chạy hệ sinh thái mã nguồn:
```powershell
dotnet run --project code/code.csproj
```

### Chạy bộ kiểm thử tự động (Regression Tests):
```powershell
dotnet run --project tests/Regression/Regression.csproj --no-launch-profile
```
Bộ kiểm thử sẽ tự động thiết lập dữ liệu mẫu độc lập, kiểm tra độ chính xác của thuật toán AHP, tính nhất quán $CR$, concurrency kho và quy trình nghiệp vụ.
