# Đưa Aurora Hotel lên GitHub và hosting miễn phí

Mục tiêu: chia sẻ đường dẫn website để thử đăng ký, đặt phòng, lễ tân, dịch vụ, thanh toán, checkout và quản trị trên cùng một SQL Server. GitHub giữ source; IIS hosting chạy ASP.NET Core. Không dùng GitHub Pages cho backend này.

## Nơi triển khai đề xuất

MonsterASP.NET Free hỗ trợ .NET 10, SQL Server và tên miền phụ của nhà cung cấp; không cần thẻ tín dụng. Trang chính thức công bố 1 website, 1 database, 5 GB file, 1 GB database và 256 MB RAM. Đây là gói thử nghiệm có giới hạn tài nguyên; chưa đo tải thực tế của Aurora trên hosting.

**HTTPS ở gói Free cần yêu cầu hỗ trợ sau khi upload website**, không tự bật ngay như Premium. Không thử đăng nhập khi URL còn HTTP. Ứng dụng Production chuyển sang HTTPS và chỉ gửi cookie qua HTTPS. Chờ chứng chỉ hoạt động rồi mới chia sẻ tài khoản demo.

Nguồn kiểm tra ngày 14/10/2026: [gói Free/Premium](https://help.monsterasp.net/getting-started/first-steps/free-vs-premium), [HTTPS Free](https://help.monsterasp.net/domains-https/https/lets-encrypt), [trang dịch vụ](https://www.monsterasp.net/). Điều kiện nhà cung cấp có thể đổi; xem lại khi tạo tài khoản.

## 1. Tài khoản và GitHub

1. Chủ dự án đăng ký tại https://www.monsterasp.net/ và xác thực email; tự hoàn tất mật khẩu, CAPTCHA và điều khoản nếu có.
2. Trong Control Panel tạo một **Free website** và **Free MSSQL database**. Chọn modern ASP.NET Core, .NET 10, IIS InProcess. Lệnh publish bên dưới dùng `win-x86`, phù hợp pool **32-bit**; nếu pool 64-bit, đổi thành `-r win-x64`.
3. Tạo repository GitHub và push source dự án. Đưa URL hosting thực tế vào mục Website/About của repository và README sau khi kiểm tra.
4. `.gitignore` loại App_Data, ảnh người dùng upload, bản build, cấu hình SQL cục bộ và publish profile. Không tải nguyên thư mục máy lên GitHub qua kéo thả; bản source ZIP đã lọc là lựa chọn an toàn hơn.

Repository của dự án: [Khoazxjee/BTLweb_1](https://github.com/Khoazxjee/BTLweb_1). Người tải mã nguồn lần đầu xem [hướng dẫn sử dụng](../HUONG_DAN_SU_DUNG.md). Workflow `.github/workflows/build.yml` build, chạy test với LocalDB và tạo artifact ZIP khi có push/PR hoặc chạy thủ công. Workflow **không tự upload hosting**, không cần mật khẩu hosting. Xem trạng thái từng lần chạy tại tab **Actions** của repository.

## 2. Tạo schema và dữ liệu demo trên database mới

Lấy **connection string** chính xác ở trang database trên hosting và cho phép remote access từ máy đang khởi tạo nếu bảng điều khiển yêu cầu. Chọn kết nối mã hóa TLS theo hướng dẫn nhà cung cấp. Database name do hosting cấp có thể khác `AuroraHotel_Development`; không thay bằng LocalDB.

Trên máy cá nhân, mở `src/AuroraHotel/appsettings.Local.json` bằng trình soạn thảo. Nếu đã có cấu hình phát triển, lưu bản riêng để khôi phục sau bước này. Nhập cấu hình hosting theo mẫu sau, thay các chỗ giữ chỗ bằng thông tin thật:

```json
{
  "ConnectionStrings": {
    "HotelDb": "CONNECTION_STRING_DATABASE_HOSTING"
  },
  "Bootstrap": {
    "DatabaseName": "TEN_DATABASE_HOSTING",
    "DemoPassword": "MAT_KHAU_ADMIN_MOI"
  }
}
```

File cấu hình cục bộ này đã được loại khỏi Git và bản publish. Mật khẩu cần ít nhất 16 ký tự, có chữ hoa, chữ thường, số và ký hiệu. Không đưa thông tin thật vào file cấu hình được theo dõi bởi Git, câu lệnh hoặc chat.

Chạy lệnh C# tại thư mục chứa solution:

```text
dotnet run --project src/AuroraHotel -c Release --no-launch-profile -- --environment Production --initialize-demo
```

Lệnh áp dụng migration và tạo 4 tài khoản cùng 12 phòng/dịch vụ/đơn minh họa. Ngày demo theo ngày chạy. **Chỉ cho phép database chưa có tài khoản/phòng**, không reset dữ liệu khi chạy lại. Không mở web server hoặc endpoint setup. Toàn bộ thao tác seed trong một transaction có khóa SQL. Nếu mật khẩu không đủ mạnh hoặc tên database không khớp, từ chối trước khi kết nối/migrate.

Tài khoản: `admin@aurora.local`, `letan@aurora.local`, `minh@aurora.local`, `linh@aurora.local`. Admin dùng mật khẩu vừa nhập; ba tài khoản còn lại có mật khẩu ngẫu nhiên riêng, khác nhau. Lệnh lưu thông tin đăng nhập tại `src/AuroraHotel/App_Data/hosting-demo-accounts.txt` trên máy chạy, đã loại khỏi Git và publish. Giữ riêng file này; chỉ gửi đúng tài khoản cho người kiểm thử được giao. Người xem thông thường tự đăng ký Customer. Đây là dữ liệu minh họa, không nhập thông tin cá nhân thật.

Khởi tạo hoàn tất, xóa phần `Bootstrap` khỏi cấu hình cục bộ và khôi phục cấu hình phát triển trước khi chạy web local. Có thể tắt remote access nếu hosting cho phép và không còn cần kết nối từ máy cá nhân. Việc sửa schema các lần sau thực hiện bằng migration riêng, không chạy lại lệnh tạo demo.

## 3. Publish và cấu hình server

```text
dotnet publish src/AuroraHotel -c Release -r win-x86 --self-contained false -p:EnvironmentName=Production -p:DebugType=None -p:DebugSymbols=false -o artifacts/hosting
```

Kết quả nằm trong `artifacts/hosting`. Trong File Explorer, chọn toàn bộ nội dung bên trong thư mục này và nén thành ZIP. Đây là **bản đã biên dịch để chạy**, khác ZIP mã nguồn dùng để nộp bài. Cấu hình project loại `App_Data`, ảnh upload, `appsettings.Local.json` và `appsettings.Development.json` khỏi bản publish; chỉ dùng thư mục đầu ra mới hoặc trống để tránh lẫn file cũ.

Cũng có thể tải artifact **AuroraHotel-hosting** từ lần chạy GitHub Actions thành công; artifact này chứa sẵn nội dung publish và được tải xuống dưới dạng ZIP.

Trong hosting, đặt Environment variables:

| Tên | Giá trị |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__HotelDb` | Connection string database hosting lấy ở bước 2 |
| `AllowedHosts` | Hostname website thực tế, không gồm `https://` hoặc đường dẫn |

Không đặt `Bootstrap__DemoPassword` trên website: chỉ dùng tạm trong tiến trình khởi tạo ở máy cá nhân. Không chuyển server sang Development để seed. Không dùng connection string LocalDB trên hosting.

Tải bản publish ZIP qua Files → Upload, giải nén nội dung **trực tiếp vào thư mục gốc ứng dụng** (nhà cung cấp gọi `/wwwroot`). Bên trong có `web.config`, `AuroraHotel.dll`, `appsettings.json` và thư mục static con `wwwroot`. Không lồng thêm thư mục tên dự án. Cho app quyền ghi thư mục static `wwwroot/uploads` để upload ảnh phòng. Khi cập nhật, giữ nguyên ảnh đã upload; gói publish không chứa hoặc xóa các ảnh đó.

Sau upload, yêu cầu HTTPS tại Control Panel → support ticket. Chủ tài khoản tự gửi nội dung sau khi thay hostname:

> Please enable HTTPS for my Free website HOSTNAME. I have uploaded an ASP.NET Core hotel management demo for coursework. The application requires HTTPS for login. Thank you.

Tôi chưa gửi ticket thay bạn. Sau khi chứng chỉ hoạt động, restart app pool và mở URL HTTPS. IIS tự xử lý thông tin proxy/HTTPS cho ASP.NET Core; không cấu hình tin mọi forwarded header từ Internet.

Hướng dẫn nhà cung cấp: [upload ZIP](https://help.monsterasp.net/websites/deploy/zip-file), [runtime/bitness](https://help.monsterasp.net/websites/configuration/scripting), [biến môi trường](https://help.monsterasp.net/websites/configuration/environment-variables).

## 4. Kiểm tra sau khi có link thật

- Trang chủ, ảnh và tìm phòng trả kết quả từ database hosting.
- Customer mới đăng ký/đặt phòng; lễ tân xác nhận; đơn trùng lịch bị từ chối.
- Check-in → hai bữa sáng → tổng 1.800.000 VND → thu đủ → checkout → phòng Dirty → Clean.
- Admin upload một ảnh: tải lại trang và restart website vẫn xem được ảnh đó.
- Customer không vào được khu nhân viên; khách A không đọc đơn khách B.
- Kiểm tra điện thoại và trình duyệt ẩn danh; không có cảnh báo chứng chỉ.
- Đăng xuất/đăng nhập sau restart; session lựa chọn phòng là bộ nhớ nên có thể mất khi hosting recycle.

Ứng dụng đã được chuẩn bị và kiểm tra cục bộ. Chưa có tài khoản hosting/hostname/SQL remote, nên **chưa có URL website công khai, chưa kiểm thử IIS thực tế hoặc giới hạn RAM của gói Free**. Mã nguồn trên GitHub và artifact của Actions không thay thế việc triển khai ứng dụng lên hosting.
