# Kiểm thử phiên bản C# / Razor

Môi trường kiểm tra: Windows, .NET SDK 10.0.400, ASP.NET Core/EF Core 10.0.11, SQL Server LocalDB. Ngày kiểm tra lại sau khi bổ sung AJAX/editor: 08/11/2026.

## Chạy kiểm thử

```text
dotnet test AuroraHotel.sln -c Release --logger "trx;LogFileName=hotel-tests.trx" --results-directory TestResults
```

Bộ kiểm thử có **28 trường hợp**. Kết quả cuối được ghi tại `TestResults/hotel-tests.trx`. Database kiểm thử có tên `AuroraHotel_Tests_GUID` hoặc `AuroraHotel_HostingTests_GUID`, được tạo riêng và dọn khi kết thúc; không xóa database phát triển.

GitHub Actions chạy trực tiếp các lệnh `dotnet` để restore, build, test và publish. Instance LocalDB mặc định tự khởi động khi ứng dụng kết nối, theo [tài liệu Microsoft](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb). Workflow tải thư mục publish lên artifact `AuroraHotel-hosting` để tải về dưới dạng ZIP.

## Phạm vi tự động

| File | Nội dung |
|---|---|
| BusinessTests.cs | Xác nhận đồng thời; chống trùng lịch; kỳ ở liền kề; điều kiện nhận phòng; giá chụp lại; dịch vụ; thu tiền/trả phòng lặp; bảo toàn lịch sử |
| HttpTests.cs | Render các màn hình; quyền; CSRF; sở hữu đơn; lựa chọn qua đăng nhập; chống sửa giá/khách từ form; REST CRUD; upload; SQL seed |
| HostingTests.cs | Production không seed ngầm; khởi tạo demo có chủ đích; kiểm tra cấu hình; cookie HTTPS |
| ServerRenderedWorkflowTests.cs | Xem trước giá không tạo đơn; trang đặt phòng/xác nhận không tải script không cần thiết; xác nhận HTML vẫn có CSRF; CSV đúng ngày/quyền và chống công thức; số phòng sẵn sàng; đổi mật khẩu và hủy phiên cũ; ngày lỗi không gây 500 |
| AjaxEditorTests.cs | AJAX trả partial, giữ lọc/phân trang và có GET fallback; validation/không có kết quả; editor lưu định dạng an toàn qua form thật, giữ dữ liệu khi lỗi và bảo toàn CSRF |

Kết quả hiện tại: **28/28 PASS**, không có trường hợp bỏ qua. Các kiểm thử chạy trên SQL Server thật để kiểm tra giao dịch và khóa, không thay bằng InMemory.

## Kiểm tra giao diện

Xem [UI-REVIEW.md](UI-REVIEW.md) cho phạm vi quan sát bằng trình duyệt. Giao diện dùng biểu mẫu server, menu HTML và CSS responsive; JavaScript chỉ thêm AJAX và editor.

## Giới hạn

Chưa kiểm thử tải lớn, máy in vật lý, thiết bị điện thoại thật hoặc triển khai bản mới lên hosting. Thanh toán kiểm thử là ghi nhận tiền tại quầy, không giao dịch với ngân hàng.
