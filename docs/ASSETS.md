# Nguồn tài nguyên

Ảnh được lưu cục bộ trong `wwwroot/images`, dùng làm minh họa cho khách sạn hư cấu, không ngụ ý những ảnh này là cơ sở tại địa chỉ demo.

| File | Nguồn |
|---|---|
| hero.jpg | https://images.unsplash.com/photo-1566073771259-6a8506099945 |
| room-deluxe.jpg | https://images.unsplash.com/photo-1611892440504-42a792e24d32 |
| room-premier.jpg | https://images.unsplash.com/photo-1590490360182-c33d57733427 |
| room-suite.jpg | https://images.unsplash.com/photo-1566665797739-1674de7a421a |
| aurora.svg | Logo vector riêng cho đồ án |

Font được lưu cục bộ trong `wwwroot/fonts`, không phụ thuộc Google Fonts khi chạy:

- **Be Vietnam Pro**: nội dung, biểu mẫu và khu vực quản trị; các độ đậm 400, 500, 600, 700. Nguồn: [Google Fonts / Be Vietnam Pro](https://github.com/google/fonts/tree/main/ofl/bevietnampro).
- **Lora**: tiêu đề trang khách hàng, có bản thường và nghiêng, độ đậm biến thiên 400–700. Nguồn: [Google Fonts / Lora](https://github.com/google/fonts/tree/main/ofl/lora).
- Các file WOFF2 giữ nguyên bộ ký tự tiếng Việt từ font gốc; giấy phép SIL Open Font License nằm cùng thư mục (`BeVietnamPro-OFL.txt`, `Lora-OFL.txt`).

Bootstrap chỉ dùng CSS; không dùng jQuery. Editor [Quill 2.0.3](https://quilljs.com/docs/installation) được lưu trong `wwwroot/lib/quill` cùng giấy phép BSD-3-Clause, lấy từ gói chính thức `quill@2.0.3` qua jsDelivr. Website tải các file này từ máy chủ của mình, không gọi CDN khi chạy. JavaScript ứng dụng nằm trong `wwwroot/js/room-search.js` và `room-editor.js`. NuGet versions được pin trong csproj: EF/Identity/Testing 10.0.11, HtmlSanitizer 9.2.1039, SkiaSharp 4.153.1.

`System.Security.Cryptography.Pkcs` được pin trực tiếp ở **9.0.21** để ghi đè phiên bản 9.0.11 do SqlClient kéo vào. Bản vá xử lý hai cảnh báo NuGet phát hiện khi chuẩn bị repository ngày 16/10/2026: [GHSA-2fmv-rv2c-59x7](https://github.com/advisories/GHSA-2fmv-rv2c-59x7) và [GHSA-846f-f764-76mx](https://github.com/advisories/GHSA-846f-f764-76mx).

Tham khảo kỹ thuật chính: [Microsoft MVC và EF Core](https://learn.microsoft.com/aspnet/core/data/ef-mvc/), [Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity), [CSRF](https://learn.microsoft.com/aspnet/core/security/anti-request-forgery), [EF transactions](https://learn.microsoft.com/ef/core/saving/transactions), [SQL Server sp_getapplock](https://learn.microsoft.com/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql).
