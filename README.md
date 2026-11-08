# Aurora Hotel — quản lý khách sạn bằng C#

Ứng dụng **ASP.NET Core MVC / .NET 10**, giao diện **Razor**, dữ liệu **SQL Server**, đăng nhập và phân quyền bằng **ASP.NET Core Identity**.

Toàn bộ nghiệp vụ và xử lý biểu mẫu viết bằng **C#**. Razor tạo HTML, Bootstrap CSS tạo giao diện. JavaScript chỉ hỗ trợ tìm kiếm/phân trang bằng AJAX và editor Quill cho mô tả phòng; các thư viện được lưu cục bộ. Không cần cài Node.js/npm để chạy dự án. Đây là đồ án quản lý một khách sạn với ba vai trò: khách hàng, lễ tân và quản trị viên.

## Chạy dự án

Cần **.NET 10 SDK** và **SQL Server LocalDB** trên Windows. Mở Terminal tại thư mục chứa solution:

```text
dotnet restore AuroraHotel.sln
dotnet run --project src/AuroraHotel
```

Mở **http://localhost:5080**. Có thể mở `AuroraHotel.sln` trong Visual Studio hỗ trợ .NET 10 và chọn project AuroraHotel làm startup.

Development tự áp dụng migration và tạo dữ liệu minh họa một lần. Database mặc định là `AuroraHotel_Development`; không cần xóa database sau khi cập nhật mã nguồn.

Hướng dẫn chi tiết: [HUONG_DAN_SU_DUNG.md](HUONG_DAN_SU_DUNG.md).

Đối chiếu đề bài giảng viên: [docs/REQUIREMENTS.md](docs/REQUIREMENTS.md#đối-chiếu-đề-bài-giảng-viên--08112026). Đã bổ sung AJAX, editor cho textarea và [sơ đồ sitemap](docs/sitemap.svg); mở trang **Sitemap** trong `docs/diagrams.drawio` để chỉnh sửa sơ đồ.

## Tài khoản demo

| Email | Vai trò |
|---|---|
| admin@aurora.local | Quản trị viên |
| letan@aurora.local | Lễ tân |
| minh@aurora.local | Khách hàng |
| linh@aurora.local | Khách hàng |

Mật khẩu ngẫu nhiên được tạo lần đầu và lưu riêng ở `src/AuroraHotel/App_Data/demo-accounts.txt`. File không được đưa vào Git. Nếu đã có database, seed giữ nguyên mật khẩu và dữ liệu cũ. Mọi tài khoản có thể đổi mật khẩu từ menu **Tài khoản → Đổi mật khẩu**.

## Chức năng

| Khu vực | Chức năng |
|---|---|
| Công khai | Trang chủ, tìm/lọc/sắp xếp/phân trang phòng, xem ảnh và tiện nghi, đăng ký, đăng nhập |
| Khách hàng | Giữ lựa chọn qua đăng nhập, tính lại giá và kiểm tra lịch, gửi yêu cầu đặt phòng, xem lịch sử/chi tiết, hủy trước ngày đến, cập nhật liên hệ |
| Lễ tân | Tạo/sửa/xác nhận/từ chối/hủy đơn; hồ sơ khách; nhận phòng; ghi/sửa/xóa dịch vụ; thu đủ tiền; trả phòng; in/lưu PDF bảng kê; cập nhật dọn phòng |
| Quản trị | Toàn bộ quyền lễ tân; CRUD phòng/loại/dịch vụ; upload ảnh; khai thác phòng; tài khoản/quyền/khóa; báo cáo theo ngày và xuất CSV |
| API | Tìm phòng khả dụng và CRUD dịch vụ, kiểm tra quyền và chống giả mạo biểu mẫu |

Giá phòng và dịch vụ được chụp lại tại thời điểm nghiệp vụ. Đổi giá danh mục không làm thay đổi khoản phí cũ. Chống xác nhận trùng lịch, thu tiền lặp và truy cập đơn của người khác.

**Thanh toán là ghi nhận tiền mặt/chuyển khoản đã nhận tại quầy.** Ứng dụng chưa tích hợp ngân hàng hoặc cổng thanh toán trực tuyến.

## Cấu trúc dễ tra cứu

```text
src/AuroraHotel/
├── Program.cs              # Điểm khởi động ngắn gọn
├── Extensions/             # Đăng ký dependency, middleware, khởi tạo
├── Controllers/            # Luồng khách hàng và API
├── Areas/Staff/            # Controllers + Views của nhân viên
├── Models/
│   ├── Entities/           # Mỗi thực thể một file
│   ├── Enums/              # Trạng thái đơn/phòng/thanh toán
│   └── Identity/           # Tên các vai trò
├── ViewModels/             # Dữ liệu biểu mẫu/trang, chia theo chức năng
├── Services/
│   ├── Common/             # Đồng hồ, cấu hình, lỗi nghiệp vụ, giao dịch
│   ├── RoomAvailability/   # Kiểm tra lịch và tìm phòng
│   ├── Reservation/        # Tạo, sửa, xác nhận, hủy đơn
│   ├── FrontDesk/          # Nhận/trả phòng, dịch vụ, thu tiền
│   ├── RoomStatus/         # Dọn phòng và khai thác
│   ├── Catalog/            # Danh mục và xử lý ảnh
│   └── Reports/            # Tổng quan, báo cáo, xuất CSV
├── Data/                   # DbContext, migrations, seed
├── TagHelpers/             # Thành phần xác nhận viết bằng C#
├── Views/                  # Razor của khách + thành phần dùng chung
└── wwwroot/
    ├── css/                # base, public, workspace, responsive, print
    ├── js/                 # AJAX tìm phòng và kết nối editor với form C#
    ├── images/             # Ảnh và logo
    ├── fonts/              # Font tiếng Việt dùng cục bộ
    └── lib/                # Bootstrap CSS, Quill và giấy phép
tests/AuroraHotel.Tests/     # Kiểm thử nghiệp vụ, HTTP, hosting
docs/                       # Kiến trúc, chức năng, demo, kiểm thử
```

Chạy, kiểm thử và publish bằng các lệnh `dotnet` bên dưới hoặc Visual Studio. Sơ đồ báo cáo có sẵn trong `docs/diagrams.drawio`, mở và chỉnh sửa trực tiếp bằng draw.io.

Cách đọc code: **Controller → ViewModel → Service → DbContext/Entity → Razor View**. Không có lớp `HotelOperations` gom toàn bộ nghiệp vụ như bản trước.

## Cấu hình database

Tạo file `src/AuroraHotel/appsettings.Local.json` nếu không dùng LocalDB:

```json
{
  "ConnectionStrings": {
    "HotelDb": "Server=.\\SQLEXPRESS;Database=AuroraHotel_Development;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Thông tin thương hiệu, địa chỉ, điện thoại và múi giờ nằm trong nhóm `Hotel` của `appsettings.json`. Dữ liệu phòng và dịch vụ sửa ở khu quản trị.

## Build và kiểm thử

```text
dotnet build AuroraHotel.sln -c Release
dotnet test AuroraHotel.sln -c Release
```

Kiểm thử sử dụng database SQL Server LocalDB riêng với tên ngẫu nhiên và chỉ dọn database đó. Không thay đổi database phát triển của bạn.

Xem [kiến trúc](docs/ARCHITECTURE.md), [chức năng](docs/REQUIREMENTS.md), [kịch bản demo](docs/DEMO.md), [kiểm thử](docs/TESTING.md) và [hướng dẫn hosting](docs/HOSTING.md).

## Triển khai

Cần hosting hỗ trợ **ASP.NET Core .NET 10 + SQL Server**. Production không tự seed hoặc migrate. Cấu hình connection string bằng biến môi trường, dùng HTTPS và không đưa mật khẩu vào repository.

```text
dotnet publish src/AuroraHotel -c Release -o artifacts/publish
```

Các thư mục `bin`, `obj`, `App_Data`, ảnh upload, bản build và cấu hình riêng đã được loại khỏi Git.
