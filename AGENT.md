# Cập nhật phạm vi và tiến độ — 28/09/2026

Người dùng đã yêu cầu sửa theo các mục tiêu đã thống nhất, giữ tên **Quản lý nhập hàng và lựa chọn nhà cung cấp bằng AHP**. Tham chiếu hiện hành: [PHAM_VI_VA_KIEM_THU.md](PHAM_VI_VA_KIEM_THU.md). Các phần 15 bảng, không B2B và tiến độ “100%” bên dưới là lịch sử phiên bản đầu, không mô tả đúng bản hiện tại.

- [x] Sửa đăng nhập, băm mật khẩu, giới hạn demo Development, kiểm tra CSRF.
- [x] Phân quyền bên mua/bán, lọc dữ liệu phiếu và kỳ đánh giá theo doanh nghiệp.
- [x] AHP lọc đủ hàng, chỉnh 6 cặp tiêu chí, kiểm tra CR, lưu đầu vào lịch sử; WSM xếp hạng.
- [x] Phiếu mới chỉ tăng kho khi xác nhận; chặn xử lý lặp/xung đột; ghi lịch sử xử lý đơn.
- [x] Migration có backup; giữ nguyên 6 phiếu cũ để đối soát.
- [x] Kiểm thử SQL Server riêng và HTTP Production cho luồng chính.
- [ ] Đối soát tồn kho lịch sử với chứng từ thực tế (không tự suy đoán số dư).
- [ ] Cập nhật báo cáo Word, bổ sung nguồn/chứng cứ cho dữ liệu đánh giá và kiểm thử trình duyệt đầy đủ.

---

# AGENT.md

# ĐỒ ÁN: QUẢN LÝ NHẬP HÀNG VÀ LỰA CHỌN NHÀ CUNG CẤP BẰNG AHP

Tài liệu này dùng làm hướng dẫn cho AI/Code Agent khi phát triển hệ thống.

---

## 1. MỤC TIÊU HỆ THỐNG

Xây dựng website quản lý nhập hàng, sản phẩm, nhà cung cấp và tồn kho, kết hợp phương pháp AHP để đánh giá, xếp hạng và đề xuất nhà cung cấp phù hợp.

Mục tiêu chính:

* Quản lý người dùng và phân quyền.
* Quản lý danh mục, đơn vị tính và sản phẩm.
* Quản lý nhà cung cấp.
* Quản lý phiếu nhập và chi tiết phiếu nhập.
* Tự động cập nhật tổng tiền phiếu nhập.
* Tự động cập nhật tồn kho.
* Đánh giá nhà cung cấp theo nhiều tiêu chí.
* Tính AHP gồm trọng số, Lambda Max, CI và CR.
* Chấm điểm và xếp hạng nhà cung cấp.
* Đề xuất nhà cung cấp có kết quả tốt nhất.

AHP là điểm nhấn của đồ án nhưng không thay thế phần quản lý nghiệp vụ.

---

## 2. NGUYÊN TẮC PHÁT TRIỂN

### 2.1. Không tự ý thay đổi database

Database hiện tại đã được chốt.

Không được tự ý:

* Thêm bảng mới.
* Xóa bảng.
* Đổi tên bảng.
* Đổi tên cột.
* Đổi kiểu dữ liệu.
* Thêm quan hệ không cần thiết.
* Tạo nghiệp vụ không có trong database.

Chỉ thay đổi database khi người dùng yêu cầu rõ ràng.

### 2.2. Ưu tiên đơn giản

Đây là đồ án sinh viên làm một mình.

Ưu tiên:

* Code dễ đọc.
* Code dễ hiểu.
* Ít lớp không cần thiết.
* Không over-engineering.
* Không thêm framework hoặc thư viện không cần thiết.
* Không tạo kiến trúc phức tạp chỉ để "cho đẹp".

Không tự ý thêm:

* AI/ML.
* Microservices.
* Docker.
* Kubernetes.
* Redis.
* Message Queue.
* Real-time notification.
* Multi-warehouse.
* Thanh toán online.
* Bán hàng cho khách hàng.

---

# 3. DATABASE SQL SERVER

Database sử dụng:

**Microsoft SQL Server**

Không sử dụng MySQL/PostgreSQL nếu chưa được yêu cầu.

## 3.1. Các bảng hiện tại

Hệ thống có đúng 15 bảng:

```text
1. NguoiDung
2. DanhMuc
3. DonViTinh
4. SanPham
5. NhaCungCap
6. PhieuNhap
7. ChiTietPhieuNhap
8. TonKho
9. LichSuNhapHang
10. TieuChi
11. KyDanhGia
12. MaTranSoSanh
13. TrongSoTieuChi
14. DiemDanhGiaNCC
15. KetQuaXepHang
```

---

# 4. Ý NGHĨA TỪNG NHÓM BẢNG

## 4.1. Người dùng

### NguoiDung

Dùng để đăng nhập và phân quyền.

Các vai trò:

```text
admin
quanly
nhanvien
```

Không dùng:

```text
manager
staff
user
```

trong database nếu không có lý do bắt buộc.

---

## 4.2. Danh mục sản phẩm

### DanhMuc

Lưu loại sản phẩm.

### DonViTinh

Lưu đơn vị tính.

Ví dụ:

```text
Thùng
Kg
Cái
```

### SanPham

Lưu thông tin sản phẩm.

Quan hệ:

```text
DanhMuc 1 ----- N SanPham
DonViTinh 1 -- N SanPham
```

---

# 5. NHÀ CUNG CẤP

### NhaCungCap

Lưu:

* Mã nhà cung cấp.
* Tên nhà cung cấp.
* Người liên hệ.
* Số điện thoại.
* Email.
* Địa chỉ.
* Trạng thái.

Không thêm trường đánh giá thủ công vào bảng này.

Điểm AHP được lưu ở các bảng AHP riêng.

---

# 6. NHẬP HÀNG

## 6.1. PhieuNhap

Lưu thông tin phiếu nhập:

* Mã phiếu.
* Nhà cung cấp.
* Người tạo.
* Ngày nhập.
* Tổng tiền.
* Trạng thái.

Trạng thái:

```text
nhap
xacnhan
huy
```

## 6.2. ChiTietPhieuNhap

Lưu:

* Phiếu nhập.
* Sản phẩm.
* Số lượng.
* Đơn giá.
* Thành tiền.

`ThanhTien` là computed column:

```text
SoLuong * DonGia
```

Không lưu thủ công `ThanhTien` trong backend nếu SQL Server đã tự tính.

---

# 7. TỒN KHO

## TonKho

Mỗi sản phẩm chỉ có một dòng tồn kho.

Quan hệ:

```text
SanPham 1 ----- 1 TonKho
```

Các trường:

```text
MaTonKho
MaSanPham
SoLuongTon
NgayCapNhat
```

Không tạo thêm bảng kho khác.

Đồ án chỉ sử dụng **một kho**.

---

# 8. TRIGGER

Hiện tại sử dụng 2 trigger chính.

## 8.1. Trigger tổng tiền

Tên:

```text
trg_CapNhatTongTienPhieuNhap
```

Hoạt động khi:

```text
INSERT
UPDATE
DELETE
```

trên `ChiTietPhieuNhap`.

Mục đích:

```text
ChiTietPhieuNhap
        ↓
Tính lại tổng tiền
        ↓
PhieuNhap.TongTien
```

Không tính tổng tiền thủ công ở nhiều nơi.

---

## 8.2. Trigger tồn kho

Tên:

```text
trg_CapNhatTonKho
```

Hoạt động khi:

```text
INSERT
UPDATE
DELETE
```

trên `ChiTietPhieuNhap`.

Logic:

### INSERT

```text
Dữ liệu mới = inserted
Dữ liệu cũ = không có

Tồn kho:
+ số lượng mới
```

### DELETE

```text
Dữ liệu cũ = deleted
Dữ liệu mới = không có

Tồn kho:
- số lượng cũ
```

### UPDATE

```text
Tồn kho:
- số lượng cũ
+ số lượng mới
```

Có thể hiểu:

```text
Tồn kho mới = Tồn kho cũ - deleted + inserted
```

Nếu sửa:

```text
50 → 80
```

thì:

```text
-50 +80 = +30
```

Không tạo trigger riêng cho từng trường hợp nếu một trigger có thể xử lý cả 3.

---

# 9. LỊCH SỬ NHẬP HÀNG

## LichSuNhapHang

Dùng để lưu lịch sử thay đổi nhập hàng.

Các loại:

```text
nhap
dieuchinh
```

Bảng này đã tồn tại.

Không tự ý tạo thêm bảng lịch sử khác.

Nếu chưa cần triển khai đầy đủ, có thể giữ bảng để mở rộng sau.

---

# 10. AHP

AHP là phần quan trọng nhất của đồ án.

Không dùng TOPSIS trong phiên bản hiện tại.

Không dùng AI/ML để thay thế AHP.

## 10.1. Các tiêu chí hiện tại

Có 4 tiêu chí:

```text
1. Giá
2. Chất lượng
3. Thời gian giao hàng
4. Độ uy tín
```

Trong database:

```text
Giá                 → cost
Chất lượng          → benefit
Thời gian giao hàng → cost
Độ uy tín           → benefit
```

Ý nghĩa:

```text
cost:
giá trị thấp hơn tốt hơn

benefit:
giá trị cao hơn tốt hơn
```

---

# 11. CÁC BẢNG AHP

## 11.1. TieuChi

Lưu danh sách tiêu chí.

## 11.2. KyDanhGia

Mỗi lần thực hiện một bộ đánh giá AHP là một kỳ đánh giá riêng.

Ví dụ:

```text
Đánh giá tháng 08/2026
Đánh giá quý 3/2026
```

Không ghi đè dữ liệu của kỳ cũ.

## 11.3. MaTranSoSanh

Lưu ma trận so sánh cặp giữa các tiêu chí.

Có:

```text
MaKyDanhGia
MaTieuChiDong
MaTieuChiCot
GiaTri
```

Giá trị sử dụng thang Saaty:

```text
1
3
5
7
9
```

và các giá trị nghịch đảo:

```text
1/3
1/5
1/7
1/9
```

Các giá trị trung gian có thể sử dụng khi cần.

---

# 12. QUY TẮC MA TRẬN AHP

Ma trận có tính đối xứng nghịch đảo.

Ví dụ:

```text
Chất lượng so với Giá = 3
```

thì:

```text
Giá so với Chất lượng = 1/3
```

Đường chéo chính luôn:

```text
1
```

UI có thể cho người dùng nhập một nửa ma trận rồi tự sinh phần đối xứng.

Không để người dùng nhập hai ô đối nghịch một cách độc lập nếu có thể tránh sai dữ liệu.

---

# 13. TÍNH TOÁN AHP

Phần tính toán AHP nên thực hiện ở **backend**, không nhồi toàn bộ thuật toán vào SQL.

SQL Server dùng để:

```text
Lưu dữ liệu
Lưu trọng số
Lưu kết quả
Lưu lịch sử
```

Backend dùng để:

```text
Lấy ma trận
Tính toán AHP
Kiểm tra CR
Tính điểm NCC
Xếp hạng
Lưu kết quả
```

---

# 14. QUY TRÌNH TÍNH AHP

Thực hiện đúng thứ tự:

```text
MaTranSoSanh
      ↓
Tính tổng từng cột
      ↓
Chuẩn hóa ma trận
      ↓
Tính trọng số
      ↓
Tính Lambda Max
      ↓
Tính CI
      ↓
Tính CR
      ↓
Kiểm tra CR
      ↓
Tính điểm từng NCC
      ↓
Xếp hạng
      ↓
Lưu KetQuaXepHang
```

---

# 15. CHI TIẾT CÔNG THỨC AHP

## Bước 1: Tổng từng cột

Tính tổng tất cả giá trị trong từng cột.

## Bước 2: Chuẩn hóa

Mỗi phần tử:

```text
Giá trị chuẩn hóa =
Giá trị gốc / Tổng cột
```

## Bước 3: Trọng số

Lấy trung bình từng hàng sau khi chuẩn hóa.

```text
Wi = Tổng các giá trị trong hàng / số tiêu chí
```

Tổng các trọng số phải gần:

```text
1
```

## Bước 4: Lambda Max

Nhân ma trận ban đầu với vector trọng số.

Sau đó tính:

```text
(A × W)i / Wi
```

Lấy trung bình các kết quả:

```text
Lambda Max
```

## Bước 5: CI

```text
CI = (LambdaMax - n) / (n - 1)
```

Trong đó:

```text
n = số tiêu chí
```

## Bước 6: CR

```text
CR = CI / RI
```

Với 4 tiêu chí:

```text
RI = 0.90
```

## Bước 7: Kiểm tra

```text
CR <= 0.10
```

→ Ma trận đạt yêu cầu.

```text
CR > 0.10
```

→ Ma trận không nhất quán.

Hệ thống phải thông báo cho người dùng và không dùng trọng số đó để xếp hạng.

---

# 16. TrongSoTieuChi

Lưu:

```text
MaKyDanhGia
MaTieuChi
TrongSo
LambdaMax
CI
CR
```

Mặc dù `LambdaMax`, `CI`, `CR` là kết quả chung của kỳ đánh giá, **không tự ý thay đổi database hiện tại** nếu chưa được yêu cầu.

Backend có thể lưu cùng giá trị ở các dòng của cùng một kỳ đánh giá.

---

# 17. CHẤM ĐIỂM NHÀ CUNG CẤP

Bảng:

```text
DiemDanhGiaNCC
```

Lưu điểm của từng NCC theo từng tiêu chí.

Thang điểm:

```text
1 → 9
```

Ví dụ:

```text
NCC001
Giá            = 7
Chất lượng     = 8
Giao hàng      = 6
Uy tín         = 9
```

---

# 18. TÍNH ĐIỂM TỔNG HỢP

Không dùng TOPSIS.

Sử dụng:

**Weighted Sum Model**

Công thức:

```text
Điểm tổng =
Trọng số tiêu chí 1 × Điểm NCC tiêu chí 1
+
Trọng số tiêu chí 2 × Điểm NCC tiêu chí 2
+
...
```

Ví dụ:

```text
NCC001 = W1×Score1 + W2×Score2 + W3×Score3 + W4×Score4
```

Sau đó xếp theo:

```text
Điểm giảm dần
```

Điểm cao nhất:

```text
Xếp hạng 1
```

và:

```text
DeXuat = 1
```

---

# 19. XỬ LÝ COST / BENEFIT

Trong phiên bản hiện tại, điểm trong `DiemDanhGiaNCC` có thể được nhập theo thang 1–9, trong đó:

```text
Giá thấp hơn
→ được quy đổi thành điểm cao hơn

Chất lượng cao hơn
→ điểm cao hơn

Thời gian giao hàng thấp hơn
→ được quy đổi thành điểm cao hơn

Uy tín cao hơn
→ điểm cao hơn
```

Backend phải đảm bảo điểm đưa vào Weighted Sum có cùng hướng:

```text
điểm cao = tốt hơn
```

Không được để tiêu chí cost bị xử lý ngược khiến giá cao lại tốt hơn.

---

# 20. XẾP HẠNG

Bảng:

```text
KetQuaXepHang
```

Lưu:

```text
MaKyDanhGia
MaNhaCungCap
DiemTong
XepHang
DeXuat
```

Ví dụ:

```text
NCC003 → 7.996 → hạng 1 → DeXuat = 1
NCC001 → 7.857 → hạng 2 → DeXuat = 0
NCC002 → 6.759 → hạng 3 → DeXuat = 0
```

Không hard-code NCC được đề xuất.

Luôn lấy kết quả từ phép tính.

---

# 21. KIẾN TRÚC CODE

Có thể sử dụng:

```text
Frontend
    ↓
Backend
    ↓
SQL Server
```

Phần AHP nên tách riêng thành service/module.

Ví dụ:

```text
AHPService
```

Các hàm nên rõ ràng, dễ hiểu:

```text
TinhTongCot()
ChuanHoaMaTran()
TinhTrongSo()
TinhLambdaMax()
TinhCI()
TinhCR()
TinhDiemNCC()
XepHangNCC()
```

Không viết toàn bộ AHP thành một hàm khổng lồ.

---

# 22. NGUYÊN TẮC CODE AHP

Không sử dụng thư viện AHP có sẵn trong phiên bản đầu.

Tự viết công thức để:

* Dễ kiểm tra.
* Dễ debug.
* Dễ giải thích với giảng viên.
* Chứng minh người thực hiện hiểu thuật toán.

Kết quả backend phải khớp với ví dụ tính tay.

---

# 23. CHỨC NĂNG WEBSITE

## Nhóm 1: Đăng nhập

* Đăng nhập.
* Đăng xuất.
* Phân quyền theo `VaiTro`.

## Nhóm 2: Quản lý dữ liệu

* Danh mục.
* Đơn vị tính.
* Sản phẩm.
* Nhà cung cấp.
* Người dùng.

## Nhóm 3: Nhập hàng

* Danh sách phiếu nhập.
* Tạo phiếu nhập.
* Thêm chi tiết.
* Sửa chi tiết.
* Xóa chi tiết.
* Xem tổng tiền.
* Cập nhật tồn kho.

## Nhóm 4: AHP

* Tạo kỳ đánh giá.
* Xem 4 tiêu chí.
* Nhập ma trận so sánh.
* Tính trọng số.
* Hiển thị Lambda Max.
* Hiển thị CI.
* Hiển thị CR.
* Báo đạt/không đạt.
* Chấm điểm NCC.
* Xếp hạng NCC.
* Hiển thị NCC đề xuất.

## Nhóm 5: Dashboard

Có thể hiển thị:

```text
Tổng sản phẩm
Tổng tồn kho
Tổng nhà cung cấp
Tổng giá trị nhập
NCC đang được đề xuất
```

Không cần dashboard quá phức tạp.

---

# 24. PHẠM VI KHÔNG LÀM

Không tự ý triển khai:

```text
Bán hàng cho khách
Giỏ hàng
Thanh toán
Đặt hàng online
Giao hàng thực tế
Shipper
Nhiều kho
AI
Machine Learning
Dự báo nhu cầu
Email/SMS tự động
Zalo API
TOPSIS
```

Những nội dung này chỉ có thể xuất hiện trong phần hướng phát triển nếu cần.

---

# 25. XUẤT HÀNG

Database hiện tại tập trung vào:

```text
Nhập hàng
Tồn kho
Đánh giá nhà cung cấp
```

Không tự ý thêm `PhieuXuat` hoặc `ChiTietPhieuXuat`.

Không dùng `ChiTietPhieuNhap` để biểu diễn xuất hàng.

Nếu giảng viên hỏi:

> Tại sao không có xuất hàng?

Trả lời:

> Phạm vi đề tài tập trung vào nghiệp vụ nhập hàng, quản lý tồn kho và lựa chọn nhà cung cấp bằng AHP. Xuất hàng không phải trọng tâm của bài toán nên được giới hạn ngoài phạm vi.

---

# 26. LỊCH SỬ ĐÁNH GIÁ AHP

Mỗi lần đánh giá tạo:

```text
KyDanhGia
```

Không xóa dữ liệu cũ chỉ để tạo kết quả mới.

Ví dụ:

```text
Kỳ 1 - Tháng 08/2026
Kỳ 2 - Tháng 09/2026
Kỳ 3 - Tháng 10/2026
```

Mỗi kỳ có:

```text
MaTranSoSanh
TrongSoTieuChi
DiemDanhGiaNCC
KetQuaXepHang
```

riêng.

---

# 27. PHÂN QUYỀN

## admin

Có thể:

* Quản lý người dùng.
* Quản lý sản phẩm.
* Quản lý NCC.
* Quản lý tiêu chí.
* Xem toàn bộ hệ thống.
* Xem kết quả AHP.

## quanly

Có thể:

* Quản lý sản phẩm.
* Quản lý NCC.
* Quản lý phiếu nhập.
* Tạo kỳ đánh giá.
* Tính AHP.
* Chấm điểm NCC.
* Xem xếp hạng.
* Xem dashboard.

## nhanvien

Có thể:

* Xem sản phẩm.
* Thực hiện nghiệp vụ nhập hàng theo quyền được cấp.
* Xem tồn kho.
* Không được thay đổi trọng số AHP hoặc ma trận đánh giá nếu không được phân quyền.

Không sử dụng tên quyền `manager` hoặc `staff` khi lưu vào database.

---

# 28. DEMO AHP

Khi demo phải thể hiện được:

### Trường hợp đạt

```text
Nhập ma trận
↓
Tính
↓
CR <= 0.1
↓
Cho phép xếp hạng
```

### Trường hợp không đạt

```text
Nhập ma trận không nhất quán
↓
Tính
↓
CR > 0.1
↓
Cảnh báo
↓
Không cho dùng kết quả
```

Không hard-code:

```text
CR = 0.027
```

Mọi giá trị phải được tính từ ma trận thực tế.

---

# 29. DỮ LIỆU MẪU

Các dữ liệu mẫu hiện tại:

### Người dùng

```text
admin / Admin@123
quanly / Quanly@123
nhanvien / Nhanvien@123
```

### Danh mục

```text
Thực phẩm khô
Đồ uống
Bao bì đóng gói
```

### Đơn vị tính

```text
Thùng
Kg
Cái
```

### Sản phẩm

```text
SP001 - Mì gói Hảo Hảo
SP002 - Nước suối Aquafina
SP003 - Cà phê hòa tan G7
SP004 - Thùng carton size M
SP005 - Băng keo đóng gói
SP006 - Trà xanh không độ
```

### Nhà cung cấp

```text
NCC001 - Công ty TNHH Phân phối Trúc Hà
NCC002 - Công ty CP Thương mại Đại Phát
NCC003 - Công ty TNHH Vận tải Minh Long
```

### Tiêu chí

```text
Giá
Chất lượng
Thời gian giao hàng
Độ uy tín
```

Khi thêm dữ liệu mẫu mới phải tránh phá vỡ khóa ngoại và dữ liệu hiện có.

---

# 30. QUY TẮC KHI VIẾT SQL

SQL phải ưu tiên:

* Dễ đọc.
* Tên bảng đúng với database.
* Tên cột đúng với database.
* Không tự đổi tên thành tiếng Anh.
* Không viết tắt tên bảng khi không cần thiết.

Ví dụ ưu tiên:

```sql
JOIN ChiTietPhieuNhap
    ON TonKho.MaSanPham = inserted.MaSanPham
```

hơn là sử dụng quá nhiều alias khó hiểu.

Khi giải thích SQL cho người thực hiện, phải:

1. Dịch từng từ khóa.
2. Giải thích từng dòng.
3. Đọc cả đoạn thành câu tiếng Việt.
4. Cho ví dụ dữ liệu đơn giản.
5. Giải thích `inserted`, `deleted`, `JOIN`, `ON`, `WHERE` khi xuất hiện.

---

# 31. QUY TẮC KHI CODE

AI/Code Agent phải:

* Đọc database hiện tại trước khi code.
* Không giả định có bảng không tồn tại.
* Không giả định có cột không tồn tại.
* Không tự thêm chức năng ngoài phạm vi.
* Không tạo dữ liệu giả nếu dữ liệu đã có.
* Không đổi nghiệp vụ chỉ để code dễ hơn.
* Không tạo code trùng chức năng.
* Không tạo hai service làm cùng một việc.
* Ưu tiên giải pháp đơn giản nhất.

Nếu có nhiều cách triển khai, ưu tiên cách:

```text
ít bảng hơn
ít code hơn
dễ hiểu hơn
dễ test hơn
dễ bảo vệ hơn
```

---

# 32. QUY TẮC VỀ AHP

AHP phải được xem là module riêng nhưng gắn với nghiệp vụ nhập hàng.

Luồng chính:

```text
Nhà cung cấp
      ↓
Đánh giá nhiều tiêu chí
      ↓
AHP tính trọng số
      ↓
Kiểm tra CR
      ↓
Chấm điểm NCC
      ↓
Weighted Sum
      ↓
Xếp hạng
      ↓
Đề xuất NCC
      ↓
Hỗ trợ tạo phiếu nhập
```

AHP không tự động quyết định thay người quản lý.

Hệ thống chỉ:

```text
Phân tích
Xếp hạng
Đề xuất
```

Quyết định cuối cùng vẫn thuộc về người quản lý.

---

# 33. THỨ TỰ ƯU TIÊN KHI PHÁT TRIỂN

Phát triển theo thứ tự:

```text
1. Kết nối SQL Server
2. Đăng nhập
3. Phân quyền
4. Danh mục
5. Đơn vị tính
6. Sản phẩm
7. Nhà cung cấp
8. Phiếu nhập
9. Chi tiết phiếu nhập
10. Trigger tổng tiền
11. Trigger tồn kho
12. Kỳ đánh giá AHP
13. Ma trận so sánh
14. Tính trọng số
15. CI / CR
16. Chấm điểm NCC
17. Xếp hạng NCC
18. Dashboard
19. Kiểm thử
```

Không làm dashboard trước khi nghiệp vụ chính hoạt động.

---

# 34. KIỂM THỬ BẮT BUỘC

## Nhập hàng

Kiểm tra:

```text
Thêm chi tiết → tồn kho tăng
Sửa số lượng → tồn kho điều chỉnh
Xóa chi tiết → tồn kho giảm
```

## Tổng tiền

Kiểm tra:

```text
INSERT → tổng tiền đúng
UPDATE → tổng tiền đúng
DELETE → tổng tiền đúng
```

## AHP

Kiểm tra:

```text
Ma trận hợp lệ → CR <= 0.1
Ma trận không hợp lệ → CR > 0.1
```

## Xếp hạng

Kiểm tra:

```text
Điểm tổng đúng
Xếp hạng đúng
Chỉ NCC hạng 1 được đề xuất
```

---

# 35. KHÔNG ĐƯỢC HARD-CODE KẾT QUẢ

Không được:

```text
CR = 0.0274
NCC003 = hạng 1
DeXuat = true
```

một cách cố định.

Các kết quả phải được tính từ dữ liệu.

Có thể dùng dữ liệu mẫu để kiểm thử, nhưng kết quả cuối cùng phải do thuật toán tạo ra.

---

# 36. KHI GIẢI THÍCH CHO NGƯỜI DÙNG

Người thực hiện là sinh viên nên mọi đoạn code mới cần được giải thích dễ hiểu.

Không chỉ đưa code rồi kết thúc.

Đặc biệt với SQL:

```text
JOIN
ON
WHERE
FROM
SET
INSERT
UPDATE
DELETE
inserted
deleted
```

phải giải thích bằng tiếng Việt khi người dùng chưa hiểu.

Không dùng cách giải thích quá hàn lâm nếu có thể nói đơn giản hơn.

---

# 37. MỤC TIÊU CUỐI CÙNG

Sản phẩm cuối cùng là:

> Một website quản lý nhập hàng, tồn kho và nhà cung cấp, trong đó phương pháp AHP được sử dụng để đánh giá nhiều tiêu chí, tính trọng số, kiểm tra tính nhất quán, chấm điểm, xếp hạng và đề xuất nhà cung cấp phù hợp.

Hệ thống phải thể hiện được mối liên kết:

```text
Quản lý NCC
      ↓
AHP
      ↓
Chọn NCC
      ↓
Phiếu nhập
      ↓
Tồn kho
```

Không biến đồ án thành một hệ thống bán hàng online.

Không mở rộng phạm vi nếu chưa được người thực hiện yêu cầu.

---

# 38. NGUYÊN TẮC QUAN TRỌNG NHẤT

**Bám đúng database hiện tại.**

**Giữ đúng phạm vi đề tài.**

**Không tự ý thêm bảng.**

**AHP xử lý ở backend.**

**SQL Server lưu dữ liệu và kết quả.**

**Ưu tiên code đơn giản, dễ hiểu và dễ bảo vệ.**
# 39. TIẾN ĐỘ DỰ ÁN

Đây là phần theo dõi tiến độ chính thức của project.

AI/Code Agent phải đọc phần này trước khi thực hiện công việc và phải cập nhật sau khi hoàn thành từng giai đoạn.

## 39.1. Trạng thái

Các trạng thái sử dụng:

```text
[ ] Chưa làm
[~] Đang làm
[x] Hoàn thành
[!] Có lỗi / cần xử lý
```

## 39.2. Tiến độ hiện tại

```text
[x] 01. Chuẩn bị database SQL Server
[x] 02. Tạo 15 bảng
[x] 03. Thêm dữ liệu mẫu
[x] 04. Tạo trigger tính tổng tiền phiếu nhập
[x] 05. Tạo trigger cập nhật tồn kho
[x] 06. Đưa source code lên Git
[x] 07. Tạo AGENT.md
[x] 08. Tạo project ASP.NET Core MVC
[x] 09. Kết nối SQL Server
[x] 10. Cài Entity Framework Core
[x] 11. Tạo ApplicationDbContext
[x] 12. Tạo 15 Model Entity
[x] 13. Đăng nhập + phân quyền

[x] 14. Dashboard cơ bản
[x] 15. CRUD Danh mục
[x] 16. CRUD Đơn vị tính
[x] 17. CRUD Sản phẩm
[x] 18. CRUD Nhà cung cấp
[x] 19. Quản lý Phiếu nhập
[x] 20. Quản lý Chi tiết phiếu nhập
[x] 21. Kiểm tra trigger tổng tiền
[x] 22. Kiểm tra trigger tồn kho
[x] 23. Quản lý Tồn kho
[x] 24. Quản lý Lịch sử nhập hàng

[x] 25. Tạo Kỳ đánh giá AHP
[x] 26. Nhập Ma trận so sánh
[x] 27. Tính tổng từng cột
[x] 28. Chuẩn hóa ma trận
[x] 29. Tính trọng số
[x] 30. Tính Lambda Max
[x] 31. Tính CI
[x] 32. Tính CR
[x] 33. Kiểm tra CR
[x] 34. Chấm điểm NCC
[x] 35. Tính điểm tổng hợp Weighted Sum
[x] 36. Xếp hạng NCC
[x] 37. Đề xuất NCC

[x] 38. Liên kết AHP với Phiếu nhập
[x] 39. Dashboard hoàn chỉnh
[x] 40. Phân quyền hoàn chỉnh
[x] 41. Kiểm thử toàn hệ thống
[x] 42. Sửa lỗi & Tinh chỉnh
[x] 43. Hoàn thiện giao diện
[x] 44. Chuẩn bị dữ liệu demo
[x] 45. Chuẩn bị kịch bản bảo vệ
[x] 46. Kiểm tra lần cuối (100% Hoàn thành)
```

---

# 40. QUY TẮC CẬP NHẬT TIẾN ĐỘ

Sau mỗi chức năng hoàn thành, Agent phải:

1. Đánh dấu chức năng đó thành `[x]`.
2. Chuyển chức năng tiếp theo thành `[~]`.
3. Nếu có lỗi chưa xử lý, đánh dấu `[!]`.
4. Không đánh dấu `[x]` nếu chức năng chưa chạy được.
5. Không tự đánh dấu hoàn thành chỉ vì code đã viết xong.

Ví dụ:

```text
[x] 13. Đăng nhập + phân quyền
[~] 14. Dashboard cơ bản
[ ] 15. CRUD Danh mục
```

---

# 41. QUY TẮC LÀM VIỆC THEO GIAI ĐOẠN

Agent phải làm **từng giai đoạn**, không làm toàn bộ project trong một lần.

Thứ tự bắt buộc:

```text
Giai đoạn 1
Đăng nhập + phân quyền

        ↓

Giai đoạn 2
CRUD dữ liệu cơ bản

        ↓

Giai đoạn 3
Phiếu nhập + tồn kho

        ↓

Giai đoạn 4
AHP

        ↓

Giai đoạn 5
Dashboard

        ↓

Giai đoạn 6
Kiểm thử + hoàn thiện
```

Không được tự ý nhảy sang giai đoạn sau khi giai đoạn hiện tại còn lỗi nghiêm trọng.

---

# 42. QUY TẮC KHI HOÀN THÀNH MỖI GIAI ĐOẠN

Sau khi hoàn thành một giai đoạn, Agent phải trả báo cáo theo mẫu:

```text
GIAI ĐOẠN:
[Tên giai đoạn]

TRẠNG THÁI:
[x] Hoàn thành

ĐÃ LÀM:
- ...
- ...
- ...

FILE ĐÃ TẠO:
- ...

FILE ĐÃ SỬA:
- ...

ĐÃ TEST:
- ...

KẾT QUẢ:
- ...

LỖI CÒN LẠI:
- Không có
hoặc
- ...

TIẾN ĐỘ TIẾP THEO:
- ...
```

Không tự động làm tiếp giai đoạn tiếp theo nếu người dùng chưa yêu cầu.

---

# 43. QUY TẮC GIT

Sau mỗi chức năng hoặc nhóm chức năng lớn đã test thành công, khuyến nghị tạo commit.

Mẫu:

```text
git add .
git commit -m "Ten chuc nang"
git push
```

Ví dụ:

```text
git commit -m "Them dang nhap va phan quyen"
```

```text
git commit -m "Them quan ly san pham"
```

```text
git commit -m "Them quan ly phieu nhap va cap nhat ton kho"
```

Không commit code đang lỗi nghiêm trọng nếu chưa cần lưu để quay lại.

---

# 44. QUY TẮC KIỂM TRA TRƯỚC KHI CHUYỂN BƯỚC

Trước khi chuyển sang chức năng tiếp theo, Agent phải kiểm tra:

```text
[ ] Project build thành công
[ ] Không có lỗi compile
[ ] Database vẫn giữ nguyên
[ ] Chức năng hiện tại chạy được
[ ] Không làm hỏng chức năng cũ
[ ] Có thể test lại nếu cần
```

Chỉ khi các điều kiện trên đạt thì mới đánh dấu `[x]`.

---

# 45. CHIẾN LƯỢC ƯU TIÊN

Nếu có lỗi hoặc thiếu thời gian, ưu tiên theo thứ tự:

```text
1. Kết nối database
2. Đăng nhập + phân quyền
3. Sản phẩm
4. Nhà cung cấp
5. Phiếu nhập
6. Tồn kho
7. AHP
8. Dashboard
9. Giao diện nâng cao
10. Báo cáo phụ
```

AHP là chức năng học thuật quan trọng nên phải hoàn thành.

Không hy sinh AHP để làm các chức năng giao diện phụ.

---

# 46. MỐC HOÀN THÀNH ĐỒ ÁN

## Mốc 1 — Nền tảng

```text
[x] SQL Server
[x] 15 bảng
[x] Trigger
[x] ASP.NET Core
[x] EF Core
[x] Git
[x] AGENT.md
```

## Mốc 2 — Hệ thống cơ bản

```text
[ ] Đăng nhập
[ ] Phân quyền
[ ] Sản phẩm
[ ] Nhà cung cấp
[ ] Phiếu nhập
[ ] Tồn kho
```

## Mốc 3 — AHP

```text
[ ] Ma trận
[ ] Trọng số
[ ] Lambda Max
[ ] CI
[ ] CR
[ ] Chấm điểm NCC
[ ] Xếp hạng
[ ] Đề xuất NCC
```

## Mốc 4 — Hoàn thiện

```text
[ ] Dashboard
[ ] Kiểm thử
[ ] Sửa lỗi
[ ] Giao diện
[ ] Dữ liệu demo
[ ] Kịch bản bảo vệ
```

---

# 47. NGUYÊN TẮC CHO AGENT Ở MỌI LẦN LÀM VIỆC

Mỗi khi bắt đầu phiên làm việc:

```text
1. Đọc AGENT.md.
2. Đọc phần 39 - TIẾN ĐỘ DỰ ÁN.
3. Xác định chức năng đang [~].
4. Kiểm tra code hiện tại trước khi sửa.
5. Không làm lại chức năng đã [x].
6. Không tự ý thay đổi database.
7. Chỉ làm phạm vi người dùng yêu cầu.
8. Test sau khi code.
9. Cập nhật tiến độ.
10. Báo cáo kết quả.
```

Nếu không biết bước tiếp theo, không tự ý phát triển thêm chức năng lớn.

Hỏi người dùng hoặc dựa vào thứ tự trong phần **39.2. Tiến độ hiện tại**.

---

# 48. QUY TẮC KHÔNG LÀM QUÁ PHẠM VI

Không được tự ý biến project thành hệ thống lớn hơn đề tài.

Không tự thêm:

```text
- Website bán hàng cho khách
- Giỏ hàng
- Thanh toán
- Đơn hàng khách hàng
- Shipper
- Giao hàng
- Multi-warehouse
- AI
- Machine Learning
- TOPSIS
- Microservices
```

Nếu cần đề xuất mở rộng, chỉ đưa ra như **ý tưởng**, không tự triển khai.
