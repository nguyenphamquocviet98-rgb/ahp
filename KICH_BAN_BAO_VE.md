# Kịch bản bảo vệ

Đề tài: **Quản lý nhập hàng và lựa chọn nhà cung cấp bằng AHP**.
Phạm vi, giới hạn và kết quả kiểm thử: [PHAM_VI_VA_KIEM_THU.md](PHAM_VI_VA_KIEM_THU.md).

## Luồng demo chính

1. Đăng nhập chủ doanh nghiệp mua. Chọn sản phẩm và số lượng. Giải thích rằng nguồn chào hàng B2B là bối cảnh của nghiệp vụ nhập hàng.
2. Mở thiết lập AHP. Chọn kịch bản giá rẻ, giải thích 4 tiêu chí và 6 cặp so sánh. Chỉ các nhà cung cấp đủ lượng hàng được xét.
3. Chạy AHP, trình bày ma trận, trọng số, λmax xấp xỉ, CI, CR và bảng điểm WSM. Mở lại báo cáo để chứng minh đầu vào đã được lưu.
4. Đổi ưu tiên sang giao nhanh, so sánh thứ hạng. Giải thích không có một nhà cung cấp tốt nhất cho mọi nhu cầu.
5. Nhập ma trận mâu thuẫn để chứng minh CR > 0,10 bị chặn. Ví dụ Giá/Giao = 9, Giá/Uy tín = 1/9, Giao/Uy tín = 9; ba cặp còn lại bằng 1.
6. Từ phiên hợp lệ, tạo đơn mua. Nếu giá đã thay đổi, hệ thống yêu cầu kiểm tra lại báo giá trước khi đặt.
7. Đăng nhập chủ bên bán: duyệt đơn, kiểm tra kho trừ đúng số lượng; chuyển giao hàng. Không hủy trực tiếp khi hàng đang vận chuyển.
8. Đăng nhập bên mua: xác nhận nhận hàng, kiểm tra kho tăng đúng một lần và nhật ký người xử lý. In chứng từ nhận hàng. Không tạo thêm phiếu nhập cho cùng đơn B2B.
9. Demo nhập ngoài hệ thống: lập phiếu mới, thêm chi tiết, kiểm tra tổng tiền; chứng minh kho chưa tăng khi nháp, chỉ tăng khi quản lý xác nhận.

Chuyển tài khoản demo chỉ sử dụng trong Development. Production phải đăng nhập bằng mật khẩu. Dùng phiếu mới cho demo, không xác nhận các phiếu phiên bản cũ đã cộng kho theo cơ chế trước đây.

## Những câu cần trả lời đúng

- **AHP dùng ở đâu?** AHP tính trọng số các tiêu chí từ ma trận so sánh cặp; WSM tổng hợp điểm nhà cung cấp đã quy đổi.
- **CR đạt có chứng minh lựa chọn đúng thực tế không?** Không. Nó đo tính nhất quán của các nhận định; độ tin cậy của dữ liệu và lựa chọn tiêu chí vẫn cần kiểm chứng.
- **Mall có được ưu tiên do trả phí không?** Không cộng điểm tự động vì Mall. Điểm uy tín được dùng như dữ liệu đầu vào riêng.
- **Tại sao không chọn nhà cung cấp giá thấp nhất?** Có thể họ thiếu hàng hoặc giao chậm so với ưu tiên người mua. Nêu dữ liệu cụ thể trong phiên demo.
- **Ai quyết định cuối cùng?** Người quản lý. Phần mềm hỗ trợ phân tích và xếp hạng.
- **Tổng tiền và tồn kho có tính hai lần không?** Phiếu nhập dùng SQL trigger làm nơi cập nhật chính. Đơn B2B cập nhật kho trong SaveChanges theo đúng trạng thái, có kiểm soát xung đột.
- **AI có thay AHP không?** Không. Nó chỉ diễn giải dữ liệu phiên. Khi không gọi được mô hình, hệ thống ghi rõ đang giải thích theo quy tắc.
- **Giới hạn?** Nhận đủ một lần, chưa xử lý trả hàng sau giao/nhận một phần/thanh toán/công nợ. Số liệu lịch sử trước nâng cấp cần đối soát.

## Minh chứng kiểm thử

Chạy `dotnet run --project tests/Regression/Regression.csproj --no-launch-profile` từ thư mục gốc. Bộ kiểm thử tạo database riêng, chạy nghiệp vụ thật và HTTP Production; không dùng số liệu kinh doanh làm dữ liệu thử.
