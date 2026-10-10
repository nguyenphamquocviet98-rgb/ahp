# Quản lý nhập hàng và lựa chọn nhà cung cấp bằng AHP

## Phạm vi đã thống nhất

Giữ nguyên tên đề tài đã đăng ký. Doanh nghiệp mua hàng là đối tượng sử dụng chính. Các nhà phân phối chào bán là nguồn dữ liệu để lựa chọn nhà cung cấp. AI, Mall và quảng cáo là chức năng phụ, không phải mục tiêu nghiên cứu chính.

Hai luồng nhập hàng:

1. **Mua từ nhà phân phối trong hệ thống:** chọn sản phẩm/số lượng → AHP → đơn mua → bên bán duyệt và trừ kho → giao → bên mua xác nhận nhận đủ hàng và tăng kho. Đơn hoàn thành là chứng từ ghi nhận lô hàng; không lập lại phiếu nhập cho cùng đơn.
2. **Nhập từ nhà cung cấp ngoài luồng B2B:** lập phiếu nháp → thêm/sửa chi tiết → quản lý xác nhận hàng đã nhận → trigger tăng kho đúng doanh nghiệp và ghi lịch sử. Phiếu nháp không tăng kho.

Giới hạn hiện tại: nhận đủ một lần; chưa xử lý nhận một phần, đổi trả sau giao, thanh toán hoặc công nợ. Không hủy trực tiếp đơn đang vận chuyển để tránh hoàn kho khi hàng chưa quay về.

## Vai trò

| Vai trò | Quyền |
|---|---|
| Quản trị (`admin`, `admin_san`) | Quản lý tài khoản, doanh nghiệp; giám sát đơn và kết quả; không thao tác mua/nhận thay doanh nghiệp |
| Chủ/quản lý (`chu_npp`, `quanly`) | Thiết lập AHP, đặt hàng, duyệt đơn bán, giao/nhận, hủy trước giao, xác nhận phiếu nhập thuộc doanh nghiệp |
| Nhân viên (`nhanvien_npp`, `nhanvien`) | Lập đơn/phiếu nháp, giao/nhận, tra cứu kho doanh nghiệp; không sửa trọng số AHP hoặc duyệt đơn bán |

Quyền được kiểm tra ở backend. Dữ liệu phiếu nhập và kỳ AHP nội bộ được lọc theo doanh nghiệp của người lập; vì vậy không tự chuyển tài khoản có lịch sử sang doanh nghiệp khác trong database.

## Phương pháp và dữ liệu AHP

- Bốn tiêu chí B2B: giá sỉ (cost), thời gian giao (cost), uy tín (benefit), tồn kho (benefit).
- Chỉ xét ứng viên đang chào bán, doanh nghiệp hoạt động, đủ toàn bộ lượng mua, giá và thời gian giao dương.
- Sáu cặp so sánh có thể chỉnh theo thang 1/9…9. Backend sinh nghịch đảo, kiểm tra đầu vào và tính trọng số theo trung bình hàng sau chuẩn hóa cột.
- Tính λmax xấp xỉ, CI, CR; chỉ xếp hạng khi CR ≤ 0,10. CR đo tính nhất quán, không xác nhận tính khách quan của đầu vào.
- Giá/giao hàng/tồn kho được min–max thành điểm 1–9. Uy tín 1–10 được quy đổi thành 1 + 8 × (uy tín − 1) / 9. Mall không tự tăng điểm.
- Tổng hợp bằng WSM: tổng (trọng số × điểm). Khi bằng điểm làm tròn 3 chữ số, ưu tiên giá thấp, giao nhanh rồi mã nhà cung cấp; kết quả này là quy tắc xử lý hòa, không phải bằng chứng vượt trội.
- Phiên mới lưu ma trận, trọng số, CR và đầu vào trong `PhienAHP_B2B.SnapshotJson`. Báo cáo cũ không lấy giá/kho hiện tại thay cho đầu vào quá khứ. Trước khi đặt hàng, hệ thống kiểm tra lại giá và tồn kho.
- Cần bổ sung khảo sát/chứng cứ thực tế cho điểm uy tín, lựa chọn tiêu chí và phương pháp chuẩn hóa trong báo cáo học thuật. Bộ dữ liệu mẫu chứng minh hoạt động phần mềm, chưa chứng minh hiệu quả kinh tế ngoài thực tế.

## Sửa đổi dữ liệu và vận hành

Chạy nâng cấp khi chuyển sang máy khác:

```powershell
powershell -File database/Apply-Upgrade.ps1
dotnet run --project code/code.csproj
```

Script tự tạo backup SQL Server, kiểm tra backup, rồi thực thi migration trong transaction. Không chạy lại script `00_INIT_DATABASE_ALL_IN_ONE.sql` trên database hiện có vì script đó xóa database.

Migration bổ sung ba cột phục vụ sửa lỗi: `PhienAHP_B2B.SnapshotJson`, `PhieuNhap.PhienBanNhapKho`, `DonHangB2B.LichSuXuLyJson`; đồng bộ hai cột ảnh đã có trong model nếu thiếu. Thay trigger kho cũ và bổ sung trigger xác nhận phiếu. Không tái tính hoặc sửa số tồn lịch sử.

Database tại máy đã được nâng cấp ngày 28/09/2026. Backup trước nâng cấp:

`C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\Backup\AHP_before_upgrade_20260928_100142.bak`

**Sáu phiếu cũ / 14 dòng chi tiết được giữ nguyên.** Cơ chế cũ đã cộng kho lúc nhập chi tiết dù phiếu còn nháp. Các phiếu này mang phiên bản 1 và chỉ đọc để tránh cộng lại. Phiếu mới mang phiên bản 2. Cần đối soát số dư kho thực tế trước khi dùng số liệu lịch sử làm số dư kinh doanh; không suy đoán hoặc tự sửa số lượng từ dữ liệu cũ.

Mật khẩu mới được băm bằng ASP.NET PasswordHasher. Mật khẩu cũ chỉ được nâng cấp sau đăng nhập đúng mật khẩu hiện có, không còn chấp nhận mật khẩu demo thay thế. QuickSwitch chỉ hoạt động trong Development, qua POST có CSRF. Production không tự seed tài khoản mẫu.

Đơn B2B ghi nhật ký người thao tác/thời gian/trạng thái trong cùng lần lưu. EF sử dụng trạng thái đơn và số lượng kho làm concurrency token; xung đột được rollback và yêu cầu tải lại.

## Kiểm chứng

```powershell
dotnet run --project tests/Regression/Regression.csproj --no-launch-profile
```

Bộ kiểm thử tạo database ngẫu nhiên riêng trên SQL Server cấu hình trong `appsettings.json`, tạo dữ liệu mẫu, chạy migration hai lần, rồi xóa đúng database kiểm thử. HTTP chạy Kestrel ở Production trên cổng localhost tạm; không gọi Gemini và không thao tác database nghiệp vụ.

Đã kiểm chứng: AHP/WSM với phép tính tay, dữ liệu sai/NaN, CR không đạt, thiếu hàng, đổi ưu tiên, Mall không tăng điểm, snapshot bất biến, băm mật khẩu, quyền doanh nghiệp, duyệt/nhận/hủy lặp, xung đột cập nhật kho, phiếu nháp/xác nhận/hủy, tổng tiền SQL, Razor các trang chính, cookie đăng nhập, CSRF và khóa QuickSwitch ở Production.

Chưa có kiểm thử trình duyệt tương tác đầy đủ trên các kích thước màn hình; chưa xác minh cuộc gọi Gemini thật. Báo cáo Word cũ cần cập nhật theo phạm vi và công thức tại tài liệu này trước khi nộp.
