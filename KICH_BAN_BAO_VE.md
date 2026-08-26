# KỊCH BẢN BẢO VỆ ĐỒ ÁN & TÀI LIỆU THUYẾT TRÌNH

**Đề tài:** HỆ THỐNG QUẢN LÝ NHẬP HÀNG VÀ LỰA CHỌN NHÀ CUNG CẤP BẰNG PHƯƠNG PHÁP AHP (ANALYTIC HIERARCHY PROCESS)  
**Công nghệ:** ASP.NET Core MVC (.NET 8/9), Entity Framework Core, SQL Server, HTML5/CSS3/Bootstrap 5  

---

## PHẦN 1: GIỚI THIỆU TỔNG QUAN ĐỀ TÀI

### 1.1. Lý do chọn đề tài
Trong hoạt động chuỗi cung ứng và quản lý kho vận của doanh nghiệp:
- **Thực trạng:** Việc lựa chọn nhà cung cấp thường mang tính cảm tính, định tính hoặc chỉ dựa vào một yếu tố duy nhất là **Giá rẻ**, dẫn đến các rủi ro lớn về hàng kém chất lượng, giao hàng chậm trễ, uy tín thấp gây gián đoạn kinh doanh.
- **Giải pháp:** Xây dựng hệ thống quản lý nhập hàng kết hợp mô hình ra quyết định đa tiêu chí **AHP (Analytic Hierarchy Process)** của GS. Thomas L. Saaty nhằm định lượng hóa và tối ưu hóa việc lựa chọn đối tác cung ứng dựa trên 4 tiêu chí cốt lõi:
  1. **Giá cả (Cost)**: Giá thành và chiết khấu.
  2. **Chất lượng hàng hóa (Benefit)**: Quy cách, độ tươi mới, độ bền.
  3. **Thời gian giao hàng (Cost)**: Tốc độ phản hồi và tỷ lệ giao đúng hẹn.
  4. **Độ uy tín & Dịch vụ (Benefit)**: Lịch sử hợp tác, chính sách hậu mãi.

### 1.2. Mục tiêu đạt được
- Số hóa toàn diện quy trình nhập hàng, lập chứng từ và theo dõi biến động tồn kho theo thời gian thực.
- Thuật toán AHP được triển khai thuần toán học tại **Backend C# (.NET)**, đảm bảo tính minh bạch, có thể giải trình chi tiết từng bước (Tổng cột &rarr; Chuẩn hóa &rarr; Trọng số $W_i$ &rarr; $\lambda_{max}$ &rarr; $CI$ &rarr; $CR \le 0.10$ &rarr; Weighted Sum &rarr; Đề xuất).
- Phân quyền chặt chẽ 3 vai trò: **Quản trị viên (Admin)**, **Quản lý (quanly)**, **Nhân viên (nhanvien)**.

---

## PHẦN 2: KIẾN TRÚC HỆ THỐNG & CƠ SỞ DỮ LIỆU

### 2.1. Cấu trúc CSDL (15 Bảng SQL Server)
1. **Phân hệ Người dùng & Phân quyền:** `NguoiDung`.
2. **Phân hệ Hàng hóa:** `DanhMuc`, `DonViTinh`, `SanPham`.
3. **Phân hệ Nhà cung cấp:** `NhaCungCap`.
4. **Phân hệ Nhập hàng & Kho:** `PhieuNhap`, `ChiTietPhieuNhap`, `TonKho`, `LichSuNhapHang`.
5. **Phân hệ AHP:** `KyDanhGia`, `TieuChi`, `MaTranSoSanh`, `TrongSoTieuChi`, `DiemDanhGiaNCC`, `KetQuaXepHang`.

### 2.2. Cơ chế Trigger & Toàn vẹn dữ liệu
- **Trigger Tổng tiền (`trg_CapNhatTongTienPhieuNhap`):** Tự động tính toán lại `PhieuNhap.TongTien` khi thêm, sửa hoặc xóa dòng `ChiTietPhieuNhap`.
- **Trigger Tồn kho (`trg_CapNhatTonKho`):** Tự động đồng bộ số lượng `TonKho.SoLuongTon` và lưu vết vào `LichSuNhapHang`.

---

## PHẦN 3: GIẢI TRÌNH TOÁN HỌC THUẬT TOÁN AHP

### Bước 1: Ma trận so sánh cặp (Pairwise Comparison Matrix $A$)
Người ra quyết định đánh giá mức độ quan trọng giữa các tiêu chí theo thang đo Saaty 1–9:
- Tính chất ma trận: $a_{ij} = \frac{1}{a_{ji}}$ và $a_{ii} = 1$.

### Bước 2: Tính tổng từng cột (Column Sum)
$$S_j = \sum_{i=1}^{n} a_{ij}$$

### Bước 3: Chuẩn hóa ma trận (Normalization)
$$\overline{a}_{ij} = \frac{a_{ij}}{S_j}$$

### Bước 4: Tính vector trọng số (Priority Vector / Weights $W_i$)
$$W_i = \frac{1}{n} \sum_{j=1}^{n} \overline{a}_{ij} \quad \text{với} \quad \sum_{i=1}^{n} W_i = 1$$

### Bước 5: Tính giá trị riêng lớn nhất ($\lambda_{max}$)
Nhân ma trận ban đầu $A$ với vector trọng số $W$:
$$(A \times W)_i = \sum_{j=1}^{n} a_{ij} \cdot W_j$$
$$\lambda_{max} = \frac{1}{n} \sum_{i=1}^{n} \frac{(A \times W)_i}{W_i}$$

### Bước 6: Tính chỉ số nhất quán (Consistency Index - $CI$)
$$CI = \frac{\lambda_{max} - n}{n - 1}$$
*(Với $n = 4$ tiêu chí: $CI = \frac{\lambda_{max} - 4}{3}$)*

### Bước 7: Tính và kiểm tra tỷ số nhất quán (Consistency Ratio - $CR$)
$$CR = \frac{CI}{RI}$$
*(Với $n = 4$, chỉ số ngẫu nhiên chuẩn của Saaty $RI = 0.90$)*
- **Điều kiện ĐẠT:** $CR \le 0.10$ (Tính đánh giá có tính logic và nhất quán, được chấp nhận).
- **Nếu $CR > 0.10$:** Ma trận bị mâu thuẫn, hệ thống cảnh báo và yêu cầu đánh giá lại.

### Bước 8: Tính điểm tổng hợp (Weighted Sum Model - WSM) & Đề xuất
- Điểm đánh giá năng lực của nhà cung cấp $k$ theo tiêu chí $i$ là $D_{ki} \in [1, 9]$.
$$\text{Score}_k = \sum_{i=1}^{n} (W_i \times D_{ki})$$
- Sắp xếp $\text{Score}_k$ giảm dần: Đối tác đạt **Hạng 1** được hệ thống tự động gán cờ `DeXuat = 1` (Ưu tiên nhập hàng số 1).

---

## PHẦN 4: KỊCH BẢN DEMO 5 BƯỚC TRƯỚC HỘI ĐỒNG

| Bước | Hành động Demo | Điểm nhấn cần trình bày |
| :---: | :--- | :--- |
| **1** | **Đăng nhập & Tổng quan**<br>Truy cập `/Account/Login`, bấm nút Demo `quanly` hoặc `admin` để tự điền form. | Giới thiệu giao diện hiện đại, bảo mật Cookie Authentication, phân quyền 3 vai trò. Dashboard hiển thị 5 thẻ KPI, Widget Spotlight AHP và cảnh báo tồn kho. |
| **2** | **Quản lý Dữ liệu Cơ bản**<br>Xem Danh mục, Đơn vị tính, Sản phẩm, Nhà cung cấp. | Chứng minh hệ thống đã có đầy đủ dữ liệu thực tế, kiểm soát toàn vẹn (chặn xóa sản phẩm khi đã có phiếu nhập liên kết). |
| **3** | **Trọng tâm AHP**<br>Truy cập **Đánh giá AHP &rarr; Kỳ đánh giá** &rarr; Xem Kỳ Quý hiện tại &rarr; Bấm **Ma trận so sánh**. | **Điểm ăn tiền:**<br>- Bấm nút *Điền ma trận mẫu chuẩn (CR &le; 0.1)* &rarr; Giải thích thang đo Saaty 1-9 và cơ chế tự động điền nghịch đảo $1/a$.<br>- Xem Bảng giải trình: Chỉ số $\lambda_{max}$, $CI$, $CR = 0.054 \le 0.10$ (Badge xanh ĐẠT).<br>- Chuyển sang Chấm điểm đối tác &rarr; Xem preview điểm WSM thời gian thực.<br>- Xem Bảng xếp hạng & Banner vinh danh đối tác Hạng 1. |
| **4** | **Liên kết Thông minh AHP & Nhập hàng**<br>Từ trang xếp hạng AHP, bấm **Tạo Phiếu Nhập Hàng Từ Đối Tác Này**. | Form tự động chọn đúng Nhà cung cấp đề xuất kèm huy hiệu ⭐. Thêm các dòng sản phẩm vào phiếu &rarr; Tổng tiền tự động cập nhật & Tồn kho tự động tăng lên. |
| **5** | **Kiểm tra Tồn kho & In ấn**<br>Xem Quản lý Tồn kho & Lịch sử biến động. Bấm In phiếu nhập và In báo cáo AHP. | Minh chứng hệ thống hoàn chỉnh, sẵn sàng ứng dụng thực tế vào doanh nghiệp. |

---

## PHẦN 5: BỘ 10 CÂU HỎI VẤN ĐÁP THƯỜNG GẶP CỦA HỘI ĐỒNG & CÂU TRẢ LỜI

### Câu 1: Tại sao em lại chọn phương pháp AHP mà không chọn TOPSIS, PROMETHEE hay WSM đơn thuần?
> **Trả lời:** Phương pháp AHP có ưu điểm vượt trội là **cho phép so sánh cặp từng đôi một** giữa các tiêu chí thông qua thang đo Saaty 1–9. Điều này giúp giảm thiểu sự thiên vị chủ quan và đặc biệt AHP có **cơ chế kiểm tra tỷ số nhất quán ($CR \le 0.10$)** để loại bỏ các đánh giá mâu thuẫn phi lý trước khi tính điểm tổng hợp.

### Câu 2: Trong công thức AHP, chỉ số $RI$ lấy từ đâu? Tại sao khi $n = 4$ thì $RI = 0.90$?
> **Trả lời:** $RI$ (Random Index) là chỉ số ngẫu nhiên được GS. Thomas Saaty thực nghiệm từ việc tạo ra hàng nghìn ma trận so sánh ngẫu nhiên cùng kích thước $n \times n$. Với bài toán của em có $n = 4$ tiêu chí, giá trị chuẩn theo bảng Saaty là $RI = 0.90$.

### Câu 3: Nếu tỷ số nhất quán $CR > 0.10$ thì hệ thống xử lý như thế nào?
> **Trả lời:** Hệ thống tại Backend (`AHPService.cs`) sẽ đánh giá `IsConsistent = false`, ngăn chặn việc chuyển sang bước chấm điểm nhà cung cấp, đồng thời hiển thị cảnh báo màu đỏ thông báo tỷ số $CR$ hiện tại và yêu cầu người quản lý điều chỉnh lại các ô so sánh mâu thuẫn.

### Câu 4: Làm thế nào để giải quyết việc tiêu chí có đơn vị đo khác nhau (Ví dụ: Giá là VNĐ, Thời gian là Ngày, Chất lượng là %)?
> **Trả lời:** Hệ thống đã chuẩn hóa thang điểm đánh giá năng lực về cùng một thang đo chuẩn từ **1 đến 9** theo nguyên tắc **Điểm càng cao thì càng tốt** (Với tiêu chí Cost như Giá rẻ hoặc Giao hàng nhanh thì được chấm điểm cao 8-9, còn tiêu chí Benefit như Chất lượng cao thì cũng được chấm 8-9). Sau đó nhân với vector trọng số AHP $W_i$.

### Câu 5: Hệ thống của em tính toán AHP ở đâu? Có sử dụng thư viện bên ngoài không?
> **Trả lời:** Toàn bộ thuật toán AHP được viết thuần túy bằng ngôn ngữ **C# tại Backend (.NET)** trong file `Services/AHPService.cs` không dùng thư viện ngoài. Điều này giúp code trong sáng, dễ kiểm soát và chứng minh đầy đủ thuật toán cho hội đồng.

### Câu 6: Cơ chế cập nhật tổng tiền và tồn kho được thực hiện như thế nào để đảm bảo tính toàn vẹn?
> **Trả lời:** Dữ liệu được bảo vệ qua 2 lớp: Ở tầng Cơ sở dữ liệu SQL Server có các Trigger `trg_CapNhatTongTienPhieuNhap` và `trg_CapNhatTonKho`. Ở tầng Backend C#, Controller cũng đồng bộ tính toán lại và ghi vết vào bảng `LichSuNhapHang` để phục vụ truy xuất nhật ký.

### Câu 7: Phân quyền trong hệ thống được cấu hình như thế nào?
> **Trả lời:** Hệ thống sử dụng cơ chế **Cookie Authentication & Role-Based Authorization** của ASP.NET Core. Ba vai trò gồm:
> - `admin`: Toàn quyền quản trị hệ thống và người dùng.
> - `quanly`: Quản lý nghiệp vụ kho, hàng hóa, đối tác và thực hiện kỳ đánh giá AHP.
> - `nhanvien`: Chỉ được lập phiếu nhập và xem tồn kho.

### Câu 8: Khi xóa một Nhà cung cấp hay một Sản phẩm thì hệ thống xử lý thế nào nếu đã có dữ liệu phát sinh?
> **Trả lời:** Hệ thống áp dụng nguyên tắc toàn vẹn dữ liệu: Nếu Sản phẩm/Nhà cung cấp đã từng có Phiếu nhập hoặc tham gia vào Kỳ đánh giá AHP, hệ thống sẽ chặn hành động xóa vĩnh viễn và hiển thị thông báo cảnh báo để bảo vệ dữ liệu lịch sử.

### Câu 9: Điểm mới và tính thực tiễn nổi bật nhất của đề tài là gì?
> **Trả lời:** Tính liên kết khép kín và thông minh: Sau khi thuật toán AHP tìm ra Nhà cung cấp tối ưu nhất (Hạng 1), hệ thống tự động gắn huy hiệu đề xuất và cho phép tạo ngay Phiếu nhập hàng cho đối tác này chỉ bằng 1 cú nhấp chuột, giúp tiết kiệm thời gian và tối ưu hóa chi phí cho doanh nghiệp.

### Câu 10: Hướng phát triển trong tương lai của đề tài là gì?
> **Trả lời:** Mở rộng tích hợp phương pháp Fuzzy AHP (AHP mờ) để xử lý các ý kiến đánh giá có độ mờ cao hơn của nhiều chuyên gia cùng lúc, tích hợp xuất nhập khẩu dữ liệu qua Excel/PDF và xây dựng biểu đồ dự báo nhu cầu hàng tồn kho bằng Machine Learning.
