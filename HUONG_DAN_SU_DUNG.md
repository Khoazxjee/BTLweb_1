# Hướng dẫn tải và sử dụng Aurora Hotel

Repository: [Khoazxjee/BTLweb_1](https://github.com/Khoazxjee/BTLweb_1).

Đây là website quản lý khách sạn viết bằng ASP.NET Core MVC, có backend và SQL Server. Sau khi tải mã nguồn, bạn chạy ứng dụng rồi mở địa chỉ localhost trong trình duyệt. Mở một file HTML hoặc bật GitHub Pages sẽ không chạy được các chức năng đăng nhập, đặt phòng và quản trị.

## 1. Cài môi trường trước khi chạy

Hướng dẫn nhanh bên dưới dành cho **Windows 10/11** với SQL Server LocalDB.

| Cần cài | Cách kiểm tra |
|---|---|
| [.NET SDK 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), chọn SDK cho Windows đúng kiến trúc máy | Mở Terminal, chạy `dotnet --list-sdks`; cần có dòng `10.0.xxx` |
| [SQL Server Express LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) | Chạy `sqllocaldb info` |
| Git, nếu dùng cách clone | Chạy `git --version`; không cần Git nếu tải ZIP |

**Chọn SDK, không chỉ cài Runtime.** SDK đã có công cụ build và runtime. Bạn có thể dùng Visual Studio hỗ trợ .NET 10, VS Code hoặc chỉ Terminal; SQL Server Management Studio không bắt buộc để chạy web. Sau khi cài đặt, đóng và mở lại Terminal.

LocalDB chỉ chạy trên Windows. Nếu dùng macOS/Linux hoặc SQL Server riêng, cần một SQL Server có thể truy cập và thay connection string như mục 7; bộ test đi kèm mặc định dùng LocalDB trên Windows.

## 2. Tải mã nguồn

### Cách A — không cần Git

1. Mở [repository](https://github.com/Khoazxjee/BTLweb_1).
2. Chọn **Code → Download ZIP**.
3. Giải nén bằng **Extract All** vào một thư mục bạn có quyền ghi, ví dụ `D:\BTLweb_1-main`.
4. Mở thư mục chứa `AuroraHotel.sln`, `README.md`, `src` và `docs`. Không chạy trực tiếp bên trong file ZIP.
5. Bấm vào thanh địa chỉ File Explorer, nhập `cmd`, nhấn Enter để mở Terminal tại thư mục đó.

### Cách B — dùng Git

Mở Terminal tại thư mục muốn lưu dự án rồi chạy:

```text
git clone https://github.com/Khoazxjee/BTLweb_1.git
cd BTLweb_1
```

## 3. Chạy website lần đầu

Trong Terminal, tại thư mục chứa `AuroraHotel.sln`, chạy:

```text
dotnet restore AuroraHotel.sln
dotnet run --project src/AuroraHotel
```

Chạy lần lượt, chờ mỗi lệnh thành công rồi chạy lệnh tiếp theo. Lần đầu cần Internet để tải các gói NuGet. Khi cửa sổ hiện **`Now listening on: http://localhost:5080`**, mở:

**[http://localhost:5080](http://localhost:5080)**

Bạn cũng có thể mở `AuroraHotel.sln` trong Visual Studio hỗ trợ .NET 10, đặt `AuroraHotel` làm Startup Project, chọn profile `AuroraHotel` rồi nhấn **Ctrl + F5** để chạy.

Ứng dụng dùng profile **Development**, tự tạo database `AuroraHotel_Development`, áp dụng migration và nạp dữ liệu minh họa vào database mới. Không cần tải file `.mdf`, import SQL hoặc tự tạo bảng trước. Font, ảnh minh họa và thư viện giao diện được lưu trong dự án.

**Giữ cửa sổ Terminal đang chạy.** Muốn dừng web, nhấn **Ctrl + C** trong cửa sổ này. Những lần sau, mở Terminal tại thư mục dự án và chạy lại `dotnet run --project src/AuroraHotel`. Dữ liệu đã nhập được giữ trong SQL Server.

## 4. Lấy tài khoản đăng nhập

Sau lần khởi chạy đầu tiên thành công, mở file:

```text
src/AuroraHotel/App_Data/demo-accounts.txt
```

Có thể mở bằng lệnh:

```text
notepad .\src\AuroraHotel\App_Data\demo-accounts.txt
```

| Email | Vai trò | Có thể làm gì? |
|---|---|---|
| `admin@aurora.local` | Quản trị viên | Dashboard, phòng, loại phòng, dịch vụ, tài khoản và toàn bộ nghiệp vụ lễ tân |
| `letan@aurora.local` | Lễ tân | Đặt phòng, hồ sơ khách, nhận/trả phòng, bảng kê, ghi nhận thanh toán |
| `minh@aurora.local` | Khách hàng | Tìm phòng, đặt phòng, xem đơn và hồ sơ của mình |
| `linh@aurora.local` | Khách hàng | Kiểm tra thêm một tài khoản khách độc lập |

Mật khẩu được tạo ngẫu nhiên trên máy của bạn, không nằm trên GitHub. Trong môi trường Development, bốn tài khoản demo dùng chung mật khẩu ghi trong file này. Bạn cũng có thể chọn **Tạo tài khoản** trên website; tài khoản đăng ký mới luôn là Khách hàng.

Không xóa file mật khẩu hoặc database đang có dữ liệu để đặt lại tài khoản. Seed không thay đổi mật khẩu của tài khoản đã tồn tại. Nếu máy đã có database từ một bản dự án khác, cần dùng tài khoản cũ hoặc tạo database demo riêng theo mục 7.

## 5. Thử một quy trình đầy đủ

1. **Khách hàng:** đăng nhập bằng `minh@aurora.local`, vào **Phòng & kỳ nghỉ**, chọn ngày nhận/trả và số khách, mở chi tiết phòng, chọn phòng rồi gửi yêu cầu. Đơn ban đầu là **Chờ xác nhận** và chưa giữ phòng.
2. **Lễ tân/Admin:** đăng xuất tài khoản khách rồi đăng nhập tài khoản nhân viên. Vào **Đặt phòng**, mở đơn vừa tạo và xác nhận. Hệ thống kiểm tra trùng lịch và chốt giá phòng.
3. **Nhận phòng:** chọn ngày nhận là hôm nay nếu muốn thử ngay. Phòng phải đang hoạt động và đã dọn sạch. Trong khu **Lễ tân**, thực hiện nhận phòng để mở bảng kê.
4. **Dịch vụ:** vào bảng kê, thêm dịch vụ và số lượng; kiểm tra tổng tiền phòng cộng tiền dịch vụ.
5. **Thanh toán:** chọn tiền mặt/chuyển khoản, nhập đúng tổng số tiền và xác nhận ghi nhận thanh toán. Đây là chức năng ghi nhận nội bộ của đồ án, không chuyển tiền thật qua ngân hàng.
6. **Trả phòng:** khi đã thanh toán đủ, thực hiện trả phòng. Bảng kê được đóng, phòng chuyển sang **Cần dọn**. Mở chứng từ để xem hoặc in.
7. **Chuẩn bị phòng:** vào **Tình trạng phòng**, cập nhật phòng đã dọn thành **Sạch**. Admin có thể xem số tiền thực thu trong **Tổng quan**.

Để thử hai vai trò cùng lúc, dùng một cửa sổ trình duyệt thường và một cửa sổ ẩn danh. Hai tab trong cùng phiên trình duyệt sẽ dùng chung tài khoản đăng nhập.

Ngoài luồng trên, có thể thử quản lý loại phòng/phòng/dịch vụ, tải ảnh phòng, sửa mô tả, lọc đơn, hồ sơ khách và phân quyền tài khoản. Không dùng dữ liệu cá nhân thật cho đồ án. Kịch bản kiểm tra chi tiết: [docs/DEMO.md](docs/DEMO.md).

## 6. Lỗi thường gặp

| Hiện tượng | Cách xử lý |
|---|---|
| `dotnet` không được nhận diện | Cài .NET **SDK 10**, rồi mở lại Terminal |
| Không tìm thấy SDK phù hợp / lỗi `NETSDK1045` | Kiểm tra `dotnet --list-sdks`; dự án dùng `net10.0`, không chạy với chỉ SDK 8 hoặc 9 |
| Không tìm thấy `sqllocaldb` / lỗi kết nối SQL | Cài LocalDB, mở lại Terminal, chạy `sqllocaldb info` và làm theo các lệnh bên dưới |
| `Unable to load the service index` khi restore | Kiểm tra Internet, proxy hoặc kết nối đến NuGet, rồi chạy lại restore |
| Không tìm thấy project/solution | Chuyển vào đúng thư mục chứa `AuroraHotel.sln`; với ZIP có thể cần mở thêm thư mục `BTLweb_1-main` bên trong |
| `Address already in use` ở cổng 5080 | Có một bản web khác đang chạy. Mở bản đang chạy hoặc dừng đúng cửa sổ Terminal cũ bằng Ctrl + C trước khi chạy lại |
| Không mở được localhost | Kiểm tra Terminal có dòng `Now listening on` và không có lỗi; dùng **http**, không phải https, ở chế độ local |
| Không có file tài khoản demo | Xem lỗi trong Terminal; file chỉ được tạo khi seed thành công ở Development |
| Đăng nhập sai nhiều lần | Identity có thể khóa tạm 10 phút; dùng mật khẩu từ đúng lần khởi tạo database trên máy đó |
| Đơn demo không nằm trong danh sách khách đến hôm nay | Ngày mẫu được tạo theo lần khởi tạo đầu; tạo đơn mới cho hôm nay để thử, hoặc xem toàn bộ ở **Đặt phòng** |
| Chưa thấy font/giao diện mới | Nhấn **Ctrl + F5** để tải lại CSS và tài nguyên |

Kiểm tra LocalDB:

```text
sqllocaldb info
```

Nếu danh sách **chưa có** `MSSQLLocalDB`, tạo instance:

```text
sqllocaldb create MSSQLLocalDB
```

Khởi động instance rồi chạy lại website:

```text
sqllocaldb start MSSQLLocalDB
dotnet run --project src/AuroraHotel
```

Không dùng lệnh xóa instance/database để sửa lỗi một cách tự động; thao tác đó có thể làm mất dữ liệu bạn đang cần.

## 7. Dùng database riêng hoặc SQL Server Express

Tạo file `src/AuroraHotel/appsettings.Local.json`. Ví dụ dưới đây tạo một bộ demo độc lập trong LocalDB, không thay thế database mặc định:

```json
{
  "ConnectionStrings": {
    "HotelDb": "Server=(localdb)\\MSSQLLocalDB;Database=AuroraHotel_MyDemo;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Nếu dùng instance SQL Server Express trên Windows, đổi phần server thành `Server=.\\SQLEXPRESS` trong JSON và dùng đúng tên instance thực tế. Windows Authentication yêu cầu tài khoản Windows hiện tại có quyền tạo/truy cập database. SQL Server ở máy khác cần connection string và quyền truy cập phù hợp.

File `appsettings.Local.json` đã được `.gitignore` loại khỏi Git. Không đưa mật khẩu SQL, file `App_Data`, bản sao lưu database hoặc ảnh khách hàng tải lên vào repository. Cấu hình ví dụ có `TrustServerCertificate=True` dùng cho máy phát triển; triển khai thật cần cấu hình TLS phù hợp với máy chủ.

## 8. Kiểm thử và cập nhật mã nguồn

Khi không còn tiến trình web dùng bản build cần thay thế, có thể chạy:

```text
dotnet build AuroraHotel.sln -c Release
dotnet test AuroraHotel.sln -c Release
```

Bộ kiểm thử cần LocalDB và tạo database riêng với tên ngẫu nhiên; không xóa database demo của bạn. Phạm vi kiểm thử: [docs/TESTING.md](docs/TESTING.md).

Nếu đã clone bằng Git, dừng web trước khi cập nhật:

```text
git pull --ff-only
dotnet restore AuroraHotel.sln
dotnet run --project src/AuroraHotel
```

Nếu bạn đã tự sửa code, lưu/commit thay đổi trước khi pull. Với bản ZIP, tải phiên bản mới vào thư mục khác và giữ lại file cấu hình, tài khoản cùng dữ liệu đang dùng; không ghi đè hoặc xóa dữ liệu một cách tùy tiện.

## 9. Chức năng và cách đọc mã nguồn bản C# / Razor

- **Tính lại tiền phòng:** sau khi chọn phòng và đăng nhập, sửa lịch ở rồi bấm “Tính lại giá & kiểm tra lịch”. Bước này chưa tạo đơn; bấm “Gửi yêu cầu” để đặt.
- **Xác nhận thao tác:** bấm nút thao tác để mở lời nhắc, rồi bấm nút xác nhận bên trong. Không cần JavaScript.
- **Đổi mật khẩu:** mở menu Tài khoản ở đầu trang; nhập mật khẩu hiện tại và mật khẩu mới. Đổi thành công sẽ làm hết hiệu lực các phiên cũ khác.
- **Xuất báo cáo:** Admin mở Tổng quan & báo cáo, chọn khoảng ngày, xem kết quả rồi bấm Tải CSV. File có thể mở trong Excel.
- **In/lưu PDF:** mở bảng kê → Xem & in chứng từ → nhấn Ctrl + P.
- **Đơn quá hẹn:** tại Lễ tân, điều chỉnh lịch trước khi nhận khách. Không dùng nút nhận phòng cho lịch đã qua.

Mỗi entity, enum và view model nằm trong file riêng. Bắt đầu từ `Program.cs`, sau đó đọc `Extensions`, `Controllers`, `ViewModels` và `Services` của chức năng muốn tìm. Giao diện khách ở `Views`, giao diện nhân viên ở `Areas/Staff/Views`. CSS tách thành `base`, `public`, `workspace`, `responsive` và `print`. Xem sơ đồ trong [README.md](README.md) và giải thích tại [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

Ứng dụng xử lý nghiệp vụ bằng C#; HTML/CSS là thành phần giao diện Razor. Không cần cài Node.js/npm để chạy dự án.

## 10. Chia sẻ với người khác

- Gửi link GitHub để người khác tải mã nguồn và làm theo hướng dẫn này.
- `localhost:5080` chỉ mở trên máy đang chạy ứng dụng; người khác không thể truy cập localhost của bạn từ máy của họ.
- Muốn có URL công khai để thử đầy đủ chức năng, cần triển khai ASP.NET Core và SQL Server. Xem [docs/HOSTING.md](docs/HOSTING.md). Đẩy lên GitHub chưa tự tạo website online.
