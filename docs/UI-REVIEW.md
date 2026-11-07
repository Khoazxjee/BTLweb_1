# Rà soát giao diện C# / Razor — 07/11/2026

## Thiết kế hiện tại

- Trang chủ chia phần giới thiệu và ảnh; tìm phòng ngay dưới phần mở đầu.
- Màu xanh trầm, nền sáng; font Be Vietnam Pro và Lora có hỗ trợ tiếng Việt, tải từ máy chủ.
- Khu khách: tìm phòng, ảnh/tiện nghi, thông tin kỳ ở, xem trước giá, yêu cầu đặt phòng, lịch sử và tài khoản.
- Khu nhân viên: sidebar trên desktop; menu ngang cuộn được trên điện thoại; bảng có vùng cuộn riêng.
- Thông báo thành công/lỗi bằng tiếng Việt, nhãn cho các trường nhập, focus bàn phím, link bỏ qua điều hướng.
- Xác nhận thao tác dùng details/summary và nút submit do ConfirmSubmitTagHelper tạo.
- Phần tương tác chạy bằng biểu mẫu C# và HTML; không tải JavaScript.

## Đã quan sát trong trình duyệt tích hợp

| Màn hình | Kiểm tra |
|---|---|
| Trang chủ | Ảnh, font, menu, tiêu đề, form tìm phòng, card đọc từ database |
| Điện thoại | Trang chủ và form tìm phòng; quản trị chuyển sang menu ngang, thẻ số liệu hai cột |
| Đăng nhập | Tài khoản quản trị demo đăng nhập và đến đúng khu vực |
| Dashboard | Bộ lọc ngày, thẻ số liệu, trạng thái đơn, hành động và bảng đơn gần đây |
| Lễ tân | Các tab khách đến/đang ở/đến hạn trả; đường dẫn tới chi tiết/bảng kê |
| Bảng kê | Hiển thị tiền phòng, dịch vụ, số phải thu; mở bước xác nhận HTML trước thanh toán, không ghi thêm giao dịch vào dữ liệu phát triển |
| Menu tài khoản | Mở menu, đường dẫn đổi mật khẩu và đăng xuất hoạt động |

Responsive được kiểm tra bằng viewport yêu cầu 390 × 844 và 1440 × 900. Trình duyệt tích hợp có thể điều chỉnh kích thước CSS theo mức zoom. Kiểm tra DOM ở dashboard điện thoại cho thấy chiều rộng nội dung bằng chiều rộng viewport, không tràn ngang toàn trang.

Các biểu mẫu nghiệp vụ, quyền truy cập và render những màn hình còn lại được xác minh trong kiểm thử HTTP; xem TESTING.md. Bản dữ liệu phát triển cũ được giữ nguyên nên báo cáo tháng mới có thể chưa có tiền thu.

## Giới hạn

Chưa kiểm tra trên điện thoại thật, Safari/Firefox hoặc máy in vật lý. Giao diện có CSS in A4; in/lưu PDF dùng lệnh in của trình duyệt. Bản làm lại chưa được triển khai lên hosting.
