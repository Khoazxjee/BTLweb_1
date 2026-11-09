# Chức năng của phiên bản C# / Razor

Phiên bản ngày 08/11/2026 giữ xử lý nghiệp vụ bằng C#, mã nguồn tách rõ và giao diện dễ sử dụng. Tìm kiếm/phân trang dùng AJAX với partial Razor, mô tả phòng dùng editor Quill kết nối textarea; biểu mẫu GET và textarea gốc vẫn dùng được khi JavaScript không tải. Xác nhận thao tác tiếp tục dùng HTML gốc.

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

## Đối chiếu đề bài giảng viên — 08/11/2026

Nguồn: [YeuCauBaiTapLon.pdf, 5 trang](https://drive.google.com/file/d/1NQPNslhcTx0k3nXyhvn6nrl0muoT6KDw/view). Đã đọc toàn bộ tài liệu và đối chiếu mã nguồn, tài liệu dự án, phản hồi HTTP cùng 28 kiểm thử tự động. **Đã bổ sung AJAX, editor cho textarea và sitemap**; các yêu cầu về bản báo cáo và xác nhận phiên bản framework vẫn cần hoàn thiện như bảng dưới.

| Mục đề bài | Hiện trạng | Minh chứng / việc còn thiếu |
|---|---|---|
| 2 — ASP.NET MVC | Cần đối chiếu phiên bản trong bài lab | Dự án dùng ASP.NET Core MVC / .NET 10. PDF không ghi rõ MVC 5 trên .NET Framework hay ASP.NET Core MVC; chưa đủ căn cứ khẳng định đúng phiên bản thầy yêu cầu. |
| 3.1 — Ít nhất hai nhóm người dùng | Có | Ba vai trò Customer, Receptionist, Admin; kiểm tra quyền trên controller và quyền sở hữu đơn. |
| 3.2 — Framework giao diện | Có | Layout tải Bootstrap CSS; phần giao diện kết hợp CSS riêng. Không cần Angular/React để đáp ứng mục này. |
| 3.3 — Phân trang và lọc | Có | RoomAvailabilityService dùng lọc theo ngày, giá, loại phòng, sắp xếp và Skip/Take; trang danh mục cũng có lọc/phân trang. |
| 3.3 — AJAX | Có | room-search.js dùng fetch với X-Requested-With, thay vùng kết quả bằng partial Razor; phân trang giữ bộ lọc, Back/Forward khôi phục URL/form. Hủy yêu cầu cũ, báo lỗi tải và có liên kết mở trang đầy đủ. |
| 3.3 — Thêm/sửa/xóa | Có | CRUD phòng, loại phòng, dịch vụ; chặn xóa dữ liệu đang được tham chiếu hoặc chuyển ngừng cung cấp để giữ lịch sử. |
| 3.3 — Session/cookies | Có | Session giữ lựa chọn phòng; Identity dùng cookie đăng nhập, phân quyền và đăng xuất. |
| 3.3 — Validation/Annotation | Có | ViewModel có Required, Range, StringLength, Compare, Display; controller và service kiểm tra dữ liệu phía máy chủ. |
| 3.3 — RESTful API/Service | Có | GET khả dụng phòng; GET/POST/PUT/DELETE dịch vụ, phân quyền và kiểm tra chống giả mạo cho thao tác ghi. |
| 3.3 — Upload ảnh | Có | Ảnh phòng JPEG/PNG/WebP tối đa 5 MB, kiểm tra nội dung và mã hóa lại WebP. |
| 3.3 — Editor cho textarea | Có | Quill 2.0.3 ở màn hình Thêm/Sửa phòng, có đậm/nghiêng/tiêu đề/danh sách. Đồng bộ về textarea Description, giữ nội dung khi lỗi; C# kiểm tra độ dài và lọc HTML khi lưu/hiển thị. |
| 3.3 — Kiểm thử | Có | Chạy lại ngày 08/11/2026: 28/28 PASS, không bỏ qua; gồm nghiệp vụ, HTTP, quyền, upload, API, SQL, AJAX và lưu nội dung editor. |
| 3.4 — Responsive | Có triển khai và bằng chứng kiểm tra | CSS theo breakpoint, menu/bảng điều chỉnh theo chiều rộng; ảnh và phạm vi kiểm tra ở UI-REVIEW.md. Chưa kiểm tra mọi thiết bị/trình duyệt. |
| 3.5 — Bảo mật, quốc tế hóa | Đã đề cập và có phần triển khai | ARCHITECTURE.md mô tả Identity, CSRF, XSS, upload, vi-VN, múi giờ và hướng mở rộng tài nguyên ngôn ngữ. Hiện chỉ có tiếng Việt; PDF yêu cầu tìm hiểu/đề cập, không ghi bắt buộc có nút đổi ngôn ngữ. |
| 4.2 — Mã nguồn, hướng dẫn, SQL | Có | Source, HUONG_DAN_SU_DUNG.md, schema.sql và seed-demo.sql. Script dữ liệu yêu cầu chạy Development một lần để khởi tạo Identity, có ghi rõ trong đầu file. |
| 5.2 — Use case, ERD, wireframe, sitemap | Có | diagrams.drawio có Use Case, ERD, Wireframe Customer, Wireframe Staff và Sitemap. Có bản xem nhanh sitemap.svg; ARCHITECTURE.md mô tả bảng, ràng buộc và route. |
| 4.1, 5 — Báo cáo đúng cấu trúc | Chưa có bản DOCX hoàn chỉnh trong repo | Tài liệu Markdown là nguồn tham khảo để viết báo cáo; chưa có bằng chứng khảo sát thực tế, bảng phân công nhóm hoặc bảng test case đầy đủ mục tiêu/đầu vào/dự kiến/thực tế/pass-fail và xử lý lỗi theo mẫu. |

### Phần còn cần hoàn thiện trước khi nộp

1. Viết báo cáo đúng bố cục/định dạng, bổ sung khảo sát, bảng test case và phân công thực tế của nhóm.
2. Đối chiếu project của bài lab để xác định giảng viên có yêu cầu đúng ASP.NET MVC 5/.NET Framework hay chấp nhận ASP.NET Core MVC.

Mã nguồn không có file PowerShell/Python. JavaScript cho AJAX/editor chỉ xử lý tương tác trình duyệt; backend, nghiệp vụ, validation và bảo mật tiếp tục được viết bằng C#.

### Yêu cầu tài liệu và cách nộp

- PDF yêu cầu báo cáo 15–35 trang, Times New Roman 13 hoặc tương đương, giãn dòng 1.1; lề trên/dưới 2 cm, trái/phải 1.5 cm, gáy 2 cm; không có header/footer. Bìa có trường, môn học, nhóm, lớp, giảng viên, năm.
- Báo cáo cần tổng quan/khảo sát/yêu cầu, phân tích thiết kế, giao diện và chức năng có ảnh, test case chi tiết, kết luận, phân công, tài liệu tham khảo, bài học và hướng phát triển.
- PDF nói gộp báo cáo và mã nguồn/tài liệu vào một ZIP; ảnh thông báo nộp bài do người dùng cung cấp yêu cầu riêng 4 tệp PPTX, DOCX, Code.zip và HD.pptx. Khi nộp cần theo thông báo đang áp dụng trên lớp; không coi link GitHub là thay thế các tệp phải nộp.
- Thông tin lớp/nhóm, bằng chứng khảo sát và bảng phân công cần do nhóm cung cấp; không thể suy ra từ mã nguồn.
