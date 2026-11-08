# Kịch bản demo Aurora Hotel

Các giá và hồ sơ dưới đây là minh họa. Chạy Development rồi lấy thông tin đăng nhập từ `src/AuroraHotel/App_Data/demo-accounts.txt`. Có thể mở hai cửa sổ riêng tư cho Customer và Receptionist để không đổi phiên liên tục.

## Luồng từ website đến trả phòng: 1.800.000 VND

1. Admin vào **Danh mục phòng → Thêm phòng**. Chọn Deluxe Garden (2 khách), số phòng mới chưa dùng, giá **800000**. Upload JPEG/PNG/WebP; nhập mô tả bằng editor, thử chữ đậm/danh sách và lưu. Mở lại phòng để kiểm tra định dạng đã lưu.
2. Khách chưa đăng nhập tìm từ **hôm nay** đến **sau hai ngày**, hai khách. Mở đúng phòng vừa tạo. Bấm **Tiếp tục đặt phòng**; đăng nhập `minh@aurora.local`. Session giữ kỳ ở. Kiểm tra liên hệ, bấm **Tính lại giá & kiểm tra lịch**, rồi gửi yêu cầu. Kết quả là Pending/Chờ xác nhận, **chưa giữ chỗ**.
3. Receptionist đăng nhập `letan@aurora.local`, vào Đặt phòng tìm mã vừa tạo, chọn Xác nhận. Giá chốt 800.000 VND/đêm. Có thể tạo hai Pending trước bước xác nhận để demo đơn thứ hai bị chặn khi xác nhận trùng; ca đồng thời thật được kiểm chứng tự động bằng hai DbContext/transaction độc lập.
4. Tại Lễ tân hoặc Chi tiết đơn, bấm Nhận phòng. Phòng phải Clean/Active và đúng ngày đến. Kết quả có một folio và một dòng phòng 2 × 800.000 = **1.600.000 VND**. Refresh trang không thêm folio/chi phí.
5. Chọn **Bữa sáng tại nhà hàng**, giá 100.000/suất, số lượng **2**. Tổng dịch vụ **200.000 VND**, tổng phải thu **1.800.000 VND**.
6. Thử số tiền 1.700.000 hoặc 1.900.000: bị chặn. Nhập **1800000**, chọn Tiền mặt hoặc Chuyển khoản, xác nhận đã nhận tiền. Phí bị khóa; gửi lại cùng thanh toán không sinh thêm Payment.
7. Bấm Hoàn tất trả phòng và xác nhận. Reservation CheckedOut, Folio Closed, số dư 0; phòng chuyển Dirty, không còn occupancy.
8. Xem & in chứng từ. Nhấn Ctrl + P để in hoặc lưu PDF. Nội dung có khách, phòng, kỳ ở, phí, nhân viên thu, phương thức và thời điểm; trang ghi rõ chứng từ nội bộ.
9. Tình trạng phòng → phòng vừa trả → chọn Sạch → Cập nhật. Dashboard Admin trong kỳ có ngày thu tăng đúng **1.800.000 VND**; bấm **Tải CSV** để lấy báo cáo.

## Demo nhanh từ dữ liệu seed

* `AU-DEMO-ARRIVAL`: phòng 101, Confirmed, ngày đến là ngày khởi tạo, 2 đêm giá 800.000. Phù hợp demo nhận phòng nhanh.
* `AU-DEMO-PENDING`: phòng 102, chờ xác nhận lúc mới seed.
* `AU-DEMO-INHOUSE`: phòng 201 đã có khách, có folio; phù hợp demo chặn checkout khi còn nợ.
* Phòng 104: Dirty; nhận phòng bị chặn cho đến khi đánh dấu Sạch.
* Phòng 303: OutOfOrder, không xuất hiện trong tìm kiếm đặt mới.
* Hai Customer có hồ sơ riêng để thử thay ID đơn trong URL.

**Trạng thái bàn giao ngày 14/10/2026:** trong QA trình duyệt, đơn `AU-DEMO-PENDING` đã được xác nhận, nhận phòng, thêm hai bữa sáng, thu 1.800.000, checkout và dọn sạch. Chứng từ còn nguyên để xem. Tổng tiền đã thu trong tháng từ 2.400.000 tăng lên 4.200.000 VND. Đơn `AU-DEMO-ARRIVAL` vẫn có thể dùng cho demo tiếp theo nếu còn đúng ngày nhận. Nếu demo vào ngày khác, tạo đơn mới hoặc điều chỉnh lịch hợp lệ trước check-in.

## Kiểm tra quản trị

* Sửa giá phòng sau khi Confirmed: folio vẫn dùng giá chốt; sửa đơn/chuyển phòng/lịch chủ động trước check-in mới đọc lại giá.
* Sửa giá dịch vụ sau khi đã ghi: dòng phí cũ không đổi.
* Xóa phòng có lịch sử hoặc loại phòng có tham chiếu: nhận thông báo chặn.
* Xóa dịch vụ đã sử dụng: chuyển sang ngừng cung cấp, giữ dòng folio cũ.
* Admin không tự khóa hoặc đổi quyền tài khoản đang dùng. Receptionist không vào quản trị danh mục/tài khoản/dashboard tổng hợp.
* Khách A truy cập mã số đơn khách B: 404. Đổi email liên hệ không chuyển quyền sở hữu, không đổi email đăng nhập.
