# Đồ án Lập trình Web — Quản lý Khách sạn Aurora Hotel

Hệ thống website quản lý khách sạn xây dựng theo mô hình **ASP.NET Core MVC (.NET 10)**, cơ sở dữ liệu **Microsoft SQL Server** (Entity Framework Core) và giao diện **Razor Views + Bootstrap 5**.

---

## 1. Công nghệ & Ngăn xếp kỹ thuật
* **Backend:** C# / ASP.NET Core MVC (.NET 10)
* **Cơ sở dữ liệu:** Microsoft SQL Server (LocalDB) / Entity Framework Core (Code-First)
* **Xác thực & Phân quyền:** ASP.NET Core Identity (Role-based: Admin, Receptionist, Customer)
* **Frontend:** Razor Views (.cshtml), Bootstrap 5, AJAX tìm kiếm & phân trang
* **Trình soạn thảo mô tả:** Quill.js Editor

---

## 2. Hướng dẫn cài đặt & Khởi chạy

### Yêu cầu môi trường:
* [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
* SQL Server LocalDB (mặc định đi kèm khi cài Visual Studio với gói *ASP.NET and web development*)

### Cách 1: Chạy bằng Visual Studio (Khuyên dùng)
1. Mở file **`AuroraHotel.sln`** bằng Visual Studio.
2. Đảm bảo project khởi động (Startup Project) là **`AuroraHotel`**.
3. Nhấn **F5** (hoặc **Ctrl + F5**) để khởi chạy.
4. Trình duyệt sẽ tự động mở trang web tại: **http://localhost:5080**.

### Cách 2: Chạy bằng Terminal / PowerShell
1. Mở Terminal (PowerShell hoặc CMD) tại thư mục chứa file solution:
```bash
dotnet restore AuroraHotel.sln
dotnet run --project src/AuroraHotel
```
2. Mở trình duyệt và truy cập: **http://localhost:5080**.

> 💡 **Lưu ý:** Trong lần khởi chạy đầu tiên, hệ thống sẽ **tự động tạo database `AuroraHotel_Development`** và tự động nạp sẵn dữ liệu mẫu (12 phòng, loại phòng, dịch vụ và các tài khoản demo). Bạn không cần phải tạo bảng thủ công.

---

## 3. Danh sách tài khoản đăng nhập Demo

Trang đăng nhập: **[http://localhost:5080/Account/Login](http://localhost:5080/Account/Login)**

* **Mật khẩu chung cho tất cả tài khoản:** `Admin@123`

| Email đăng nhập | Vai trò | Chức năng kiểm thử chính |
|---|---|---|
| **`admin@aurora.local`** | **Quản trị viên (Admin)** | Xem biểu đồ Dashboard doanh thu, xuất báo cáo CSV, quản lý phòng, loại phòng, dịch vụ, phân quyền & khóa tài khoản |
| **`letan@aurora.local`** | **Lễ tân (Receptionist)** | Quản lý danh sách đơn, duyệt/từ chối đơn, làm thủ tục Check-in, thêm dịch vụ phòng vào bảng kê, ghi nhận thanh toán, Check-out, xem và in hóa đơn |
| **`minh@aurora.local`** | **Khách hàng (Customer)** | Tìm kiếm & lọc phòng trống, gửi yêu cầu đặt phòng, xem lịch sử và chi tiết các đơn của mình, hủy đơn hợp lệ |
| **`linh@aurora.local`** | **Khách hàng (Customer)** | Tài khoản khách hàng thứ hai để kiểm tra tính độc lập dữ liệu giữa các khách |

---

## 4. Kịch bản chạy thử nghiệm quy trình (Demo Flow)

1. **Khách hàng đặt phòng:**
   * Truy cập trang chủ hoặc menu **Phòng & kỳ nghỉ**, chọn ngày ở và số khách $\rightarrow$ bấm **Tìm phòng trống**.
   * Chọn một phòng ưng ý, xem chi tiết và bấm **Đặt phòng**.
   * Đăng nhập với tài khoản `minh@aurora.local` / `Admin@123` để hoàn tất gửi đơn. Trạng thái đơn lúc này là **Chờ xác nhận (Pending)**.

2. **Lễ tân tiếp nhận & Duyệt đơn:**
   * Đăng xuất và đăng nhập tài khoản `letan@aurora.local` / `Admin@123`.
   * Vào menu **Lễ tân** $\rightarrow$ **Đặt phòng**, mở đơn vừa tạo và bấm **Xác nhận (Confirm)** để chốt phòng và giá.

3. **Lưu trú, Sử dụng dịch vụ & Thanh toán:**
   * Vào tab **Khách đến hôm nay** $\rightarrow$ bấm **Nhận phòng (Check-in)** để mở bảng kê thanh toán (Folio).
   * Tại bảng kê, có thể thêm các dịch vụ phát sinh (nước uống, giặt ủi, bữa sáng...).
   * Khi khách trả phòng: Chọn phương thức (Tiền mặt/Chuyển khoản) $\rightarrow$ bấm **Thanh toán đủ** $\rightarrow$ bấm **Trả phòng (Check-out)** $\rightarrow$ Mở hóa đơn để in hoặc lưu PDF.
   * Phòng sẽ tự động chuyển trạng thái sang **Cần dọn (Dirty)** trên Sơ đồ buồng phòng.

4. **Quản trị viên:**
   * Đăng nhập `admin@aurora.local` / `Admin@123`.
   * Vào **Tổng quan (Dashboard)** để xem số liệu doanh thu, số lượt khách và tải file báo cáo CSV.
   * Vào **Danh mục** để thêm/sửa/xóa phòng, loại phòng và dịch vụ.

---

## 5. Cấu trúc thư mục mã nguồn

```text
├── AuroraHotel.sln              # File Solution mở bằng Visual Studio
├── README.md                    # Hướng dẫn chi tiết dự án
├── Database/                    # Script CSDL SQL Server dự phòng
│   ├── schema.sql               # Script tạo cấu trúc bảng
│   └── seed-demo.sql            # Script nạp dữ liệu mẫu
├── docs/                        # Tài liệu đặc tả, sơ đồ thiết kế & ảnh chụp minh họa
│   ├── diagrams.drawio          # File vẽ sơ đồ Use Case, ERD CSDL & Sitemap
│   ├── sitemap.svg              # Sơ đồ điều hướng website
│   ├── ARCHITECTURE.md          # Tài liệu thiết kế kiến trúc hệ thống
│   ├── REQUIREMENTS.md          # Đặc tả yêu cầu nghiệp vụ
│   ├── DEMO.md                  # Hướng dẫn kịch bản demo
│   └── screenshots/             # Ảnh chụp các màn hình giao diện
└── src/
    └── AuroraHotel/             # Mã nguồn chính của ứng dụng
        ├── Controllers/         # Controller công khai (Rooms, Bookings, Account) & REST API
        ├── Areas/Staff/         # Khu vực làm việc của Nhân viên (Lễ tân & Admin)
        ├── Models/              # Thực thể CSDL (Entities) & Định nghĩa trạng thái (Enums)
        ├── ViewModels/          # Data Transfer Object phục vụ form & giao diện
        ├── Services/            # Tầng xử lý nghiệp vụ chính của khách sạn
        ├── Views/               # Giao diện hiển thị Razor Views
        ├── Data/                # DbContext, Migrations & Dữ liệu Seed
        ├── wwwroot/             # Tài nguyên tĩnh nội bộ (CSS, JS, Fonts, Ảnh)
        ├── appsettings.json     # Cấu hình chuỗi kết nối CSDL và thông tin khách sạn
        └── Program.cs           # Cấu hình khởi chạy ứng dụng
```
