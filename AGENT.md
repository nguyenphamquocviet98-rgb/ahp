# HƯỚNG DẪN AGENT: NÂNG CẤP ĐỀ TÀI AHP (BẢN CHUẨN 07/10/2026)

Tài liệu này là chỉ dẫn kỹ thuật dành cho AI / Code Agent khi phát triển và bảo trì hệ thống.
**Tài liệu tham chiếu gốc bắt buộc:** `../DE_XUAT_NANG_CAP_DE_TAI_AHP.md` (Phiên bản 1.1 — ngày 07/10/2026).

---

## 1. MỤC TIÊU VÀ TẦM NHÌN NÂNG CẤP

Chuyển đổi hệ thống từ phần mềm quản lý kho đơn thuần sang **Hệ thống hỗ trợ ra quyết định mua hàng và đánh giá nhà cung cấp toàn diện**:
1. **Kết nối liền mạch nghiệp vụ:** Từ phát hiện nhu cầu (cảnh báo tồn thấp) $\rightarrow$ Lập yêu cầu mua $\rightarrow$ Đánh giá AHP $\rightarrow$ Đặt hàng $\rightarrow$ Nhận hàng & Kiểm tra chất lượng $\rightarrow$ Tích lũy hiệu suất NCC cho các kỳ sau.
2. **Thuật toán AHP gắn liền dữ liệu thực tế:** Điểm đánh giá NCC được sinh từ dữ liệu giao dịch thực (tỷ lệ hàng đạt kiểm tra, tỷ lệ giao đúng hạn, đơn giá báo giá), không nhập điểm thủ công tùy tiện.
3. **Giải thích quyết định:** Hệ thống phải chỉ ra được mức đóng góp của từng tiêu chí vào điểm số tổng hợp của từng nhà cung cấp.
4. **Phân tích độ nhạy (Sensitivity Analysis):** Cho phép người dùng kiểm tra độ bền vững của quyết định khi thay đổi trọng số tiêu chí và tìm ra các điểm ngưỡng đổi thứ hạng.

---

## 2. NGUYÊN TẮC PHÁT TRIỂN KỸ THUẬT

1. **Toán học AHP nghiêm ngặt:**
   - Ma trận so sánh cặp $n \times n$ đối xứng nghịch đảo: $a_{ii} = 1, a_{ji} = 1/a_{ij}$.
   - Tính toán vector trọng số chuẩn hóa $\sum w_i = 1$.
   - Tính toán $\lambda_{max}$, $CI = \frac{\lambda_{max} - n}{n - 1}$, $CR = \frac{CI}{RI}$.
   - **Chặn tuyệt đối:** Chỉ cho phép áp dụng bộ trọng số khi $CR \le 0.10$. Nếu $CR > 0.10$, hiển thị cảnh báo mâu thuẫn để người dùng điều chỉnh.

2. **Chế độ tính toán:**
   - **Chế độ vận hành (Operational):** AHP xác định trọng số tiêu chí $\times$ Điểm quy đổi từ dữ liệu giao dịch thực tế (min-max normalization) $\rightarrow$ Xếp hạng WSM.
   - **Chế độ đối chiếu học thuật (Academic):** So sánh cặp đầy đủ cả tiêu chí và các phương án theo cấu trúc phân cấp Saaty.

3. **Toàn vẹn dữ liệu kho và kiểm tra chất lượng (KCS):**
   - Chỉ hàng đạt kiểm tra chất lượng (`SoLuongDat`) mới được ghi tăng vào tồn kho có thể bán.
   - Hàng lỗi (`SoLuongLoi`) được ghi nhận biên bản và theo dõi xử lý riêng, không cộng vào tồn bán.
   - Sử dụng Transaction và Concurrency Token trong cơ sở dữ liệu để ngăn chặn ghi sổ kho trùng lặp hoặc xung đột số dư.

4. **Lưu vết và giải thích (Audit & Snapshot):**
   - Mọi phiên AHP khi ban hành phải lưu snapshot đầy đủ dữ liệu đầu vào, ma trận so sánh, trọng số và kết quả vào `SnapshotJson` để phục vụ đối soát và truy vết lịch sử.
   - Lưu rõ lý do nếu người quản lý chọn nhà cung cấp khác với đề xuất điểm cao nhất của AHP.

---

## 3. LỘ TRÌNH TRIỂN KHAI THEO 8 CHẶNG

- **Chặng 1 & 2:** Khảo sát mã nguồn hiện có, chốt bộ 4 tiêu chí cốt lõi (Chi phí, Chất lượng, Giao đúng hạn, Đáp ứng số lượng) và quy tắc hàm điểm $u(x)$.
- **Chặng 3:** Hoàn thiện dữ liệu nền (NCC, Sản phẩm, Báo giá, Tài khoản và Phân quyền máy chủ).
- **Chặng 4:** Quy trình mua hàng xuyên suốt (Yêu cầu mua $\rightarrow$ AHP $\rightarrow$ Đặt hàng $\rightarrow$ Nhận hàng).
- **Chặng 5:** Làm chắc dữ liệu kho: Nhận hàng theo đợt, kiểm tra chất lượng đạt/lỗi, chống ghi sổ lặp.
- **Chặng 6:** Làm sâu hỗ trợ quyết định: Trình bày mức đóng góp tiêu chí, xử lý dữ liệu thiếu, phân tích độ nhạy (đổi trọng số tìm ngưỡng lật hạng).
- **Chặng 7:** Hoàn thiện giao diện: Dashboard trực quan, đồ thị độ nhạy, báo cáo lịch sử.
- **Chặng 8:** Kiểm thử hồi quy, đối chiếu công thức số học và chuẩn bị kịch bản demo bảo vệ.
