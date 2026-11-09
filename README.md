# Đồ án Quản lý Khách sạn — Aurora Hotel

Ứng dụng web quản lý khách sạn xây dựng bằng **ASP.NET Core MVC** và **SQL Server** (Entity Framework Core).

## 1. Công nghệ sử dụng
- Backend: C# / ASP.NET Core MVC
- Cơ sở dữ liệu: Microsoft SQL Server (LocalDB) / Entity Framework Core
- Xác thực & Phân quyền: ASP.NET Core Identity
- Giao diện: Razor Views, Bootstrap 5, AJAX tìm kiếm

## 2. Hướng dẫn chạy dự án
Yêu cầu máy cài đặt **.NET 10 SDK** và **SQL Server LocalDB**.

Mở Terminal tại thư mục này và chạy lệnh:
```bash
dotnet restore AuroraHotel.sln
dotnet run --project src/AuroraHotel
```
Truy cập trình duyệt theo địa chỉ: **http://localhost:5080**

## 3. Tài khoản demo
Hệ thống sử dụng chung một mật khẩu chuẩn cho tất cả các tài khoản demo:
- **Mật khẩu chung:** `Admin@123`

| Email | Vai trò | Chức năng chính |
|---|---|---|
| `admin@aurora.local` | Quản trị viên (Admin) | Toàn quyền: Thống kê doanh thu, quản lý phòng, loại phòng, dịch vụ, phân quyền tài khoản |
| `letan@aurora.local` | Lễ tân (Receptionist) | Quản lý đặt phòng, Check-in, gọi dịch vụ, thu tiền, Check-out |
| `minh@aurora.local` | Khách hàng (Customer) | Tìm phòng, gửi yêu cầu đặt phòng, xem lịch sử đơn |

## 4. Cấu trúc thư mục
- `Database/`: Chứa file script CSDL (`schema.sql` và `seed-demo.sql`).
- `src/AuroraHotel/`: Toàn bộ mã nguồn chương trình (Controllers, Views, Models, Services, wwwroot...).
