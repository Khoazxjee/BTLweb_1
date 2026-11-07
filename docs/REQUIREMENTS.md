# Chức năng của phiên bản C# / Razor

Phiên bản ngày 07/11/2026 ưu tiên yêu cầu mới: xử lý nghiệp vụ bằng C#, mã nguồn tách rõ và giao diện dễ sử dụng. Tìm kiếm gửi biểu mẫu GET, xác nhận thao tác dùng HTML gốc, mô tả phòng nhập bằng textarea. Không dùng AJAX hoặc trình soạn thảo JavaScript của bản trước.

| Nhóm | Đã triển khai | Nơi xử lý |
|---|---|---|
| Xác thực | Đăng ký, đăng nhập, đăng xuất, đổi mật khẩu | AccountController + Identity |
| Tìm phòng | Ngày ở, số khách, loại, khoảng giá, sắp xếp, phân trang | RoomAvailabilityService |
| Đặt phòng | Chọn phòng, lưu lựa chọn qua đăng nhập, xem trước giá, tạo yêu cầu | BookingsController + ReservationService |
| Xử lý đơn | Tạo tại quầy, sửa lịch/phòng, xác nhận, từ chối, hủy | ReservationsController |
| Khách hàng | Hồ sơ, liên hệ, lịch sử đặt phòng | GuestsController / AccountController |
| Lưu trú | Nhận phòng, mở bảng kê, dịch vụ, thu đủ tiền, trả phòng | FrontDeskService |
| Phòng | Sạch/cần dọn; hoạt động/ngừng sử dụng; kiểm tra điều kiện nhận khách | RoomStatusService |
| Danh mục | CRUD phòng/loại/dịch vụ, ảnh tối đa 5 MB, lọc HTML mô tả | CatalogService + RoomImageService |
| Phân quyền | Customer, Receptionist, Admin; khóa tài khoản; bảo vệ admin cuối | UsersController |
| Báo cáo | Tổng tiền thực thu, số đơn, trạng thái, phòng sẵn sàng, CSV | ReportService |
| Chứng từ | Bảng kê chi tiết và CSS in; trình duyệt in/lưu PDF | FrontDesk/Invoice |
| API | Khả dụng phòng; GET/POST/PUT/DELETE dịch vụ | Controllers/Api |

## Quy tắc chính

- Đơn chờ chưa giữ chỗ. Khi xác nhận phải kiểm tra lại khả dụng và chốt giá.
- Hai kỳ ở liền kề được phép; hai kỳ đã giữ chỗ chồng nhau bị chặn.
- Nhận phòng đúng ngày đến, phòng sạch/hoạt động và chưa có khách.
- Khách chỉ xem và hủy đơn của mình; chỉ hủy trước ngày đến.
- Chỉ sửa dịch vụ trước khi thanh toán. Không cho sửa/xóa dòng tiền phòng.
- Thanh toán đúng số tiền phải thu; gửi lại cùng thanh toán không thu thêm.
- Chỉ trả phòng khi đã thu đủ; sau trả phòng chuyển sang cần dọn.
- Phòng có lịch sử không bị xóa; dịch vụ đã dùng được ngừng cung cấp.
- Tài khoản đăng ký công khai luôn là Customer.

## Phạm vi

Phù hợp đồ án quản lý một khách sạn. Chưa có đa chi nhánh, đặt nhiều phòng trong một đơn, đặt cọc/hoàn tiền, cổng thanh toán, gửi email/SMS và hóa đơn điện tử.
