# Kiểm thử phiên bản C# / Razor

Môi trường kiểm tra: Windows, .NET SDK 10.0.400, ASP.NET Core/EF Core 10.0.11, SQL Server LocalDB. Ngày kiểm tra: 07/11/2026.

## Chạy kiểm thử

```powershell
dotnet test AuroraHotel.sln -c Release --logger "trx;LogFileName=hotel-tests.trx" --results-directory TestResults
```

Bộ kiểm thử có **25 trường hợp**. Kết quả cuối được ghi tại `TestResults/hotel-tests.trx`. Database kiểm thử có tên `AuroraHotel_Tests_GUID` hoặc `AuroraHotel_HostingTests_GUID`, được tạo riêng và dọn khi kết thúc; không xóa database phát triển.

## Phạm vi tự động

| File | Nội dung |
|---|---|
| BusinessTests.cs | Xác nhận đồng thời; chống trùng lịch; kỳ ở liền kề; điều kiện nhận phòng; giá chụp lại; dịch vụ; thu tiền/trả phòng lặp; bảo toàn lịch sử |
| HttpTests.cs | Render các màn hình; quyền; CSRF; sở hữu đơn; lựa chọn qua đăng nhập; chống sửa giá/khách từ form; REST CRUD; upload; SQL seed |
| HostingTests.cs | Production không seed ngầm; khởi tạo demo có chủ đích; kiểm tra cấu hình; cookie HTTPS |
| ServerRenderedWorkflowTests.cs | Xem trước giá không tạo đơn; giao diện không tải script; xác nhận HTML vẫn có CSRF; CSV đúng ngày/quyền và chống công thức; số phòng sẵn sàng; đổi mật khẩu và hủy phiên cũ; ngày lỗi không gây 500 |

Mốc kiểm thử sau tái cấu trúc: **25/25 PASS**, không có trường hợp bỏ qua. Các kiểm thử chạy trên SQL Server thật để kiểm tra giao dịch và khóa, không thay bằng InMemory.

## Kiểm tra giao diện

Xem [UI-REVIEW.md](UI-REVIEW.md) cho phạm vi quan sát bằng trình duyệt. Giao diện dùng biểu mẫu server, menu HTML và CSS responsive; không có JavaScript ứng dụng.

## Giới hạn

Chưa kiểm thử tải lớn, máy in vật lý, thiết bị điện thoại thật hoặc triển khai bản mới lên hosting. Thanh toán kiểm thử là ghi nhận tiền tại quầy, không giao dịch với ngân hàng.
