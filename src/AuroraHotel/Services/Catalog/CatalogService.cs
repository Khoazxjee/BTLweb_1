using System.Text.RegularExpressions;
using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Services;

/// <summary>
/// Service quản lý danh mục: phòng, loại phòng và dịch vụ khách sạn.
/// </summary>
public class CatalogService
{
    private readonly HotelDbContext _db;
    private readonly HotelTransaction _transaction;
    private readonly RoomImageService _images;

    public CatalogService(HotelDbContext db, HotelTransaction transaction, RoomImageService images)
    {
        _db = db;
        _transaction = transaction;
        _images = images;
    }

    // Làm sạch chuỗi HTML mô tả phòng để tránh mã độc script
    public static string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return "";

        // Sử dụng Regex loại bỏ thẻ script, sự kiện onclick/onerror và thẻ img
        var clean = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
        clean = Regex.Replace(clean, @"on\w+\s*=\s*(['""]).*?\1", "", RegexOptions.IgnoreCase);
        clean = Regex.Replace(clean, @"<img[^>]*>", "", RegexOptions.IgnoreCase);
        return clean;
    }

    // Thêm mới hoặc cập nhật thông tin phòng
    public async Task<int> SaveRoom(RoomInput input)
    {
        // 1. Kiểm tra giá phòng hợp lệ
        if (input.NightlyRate <= 0 || input.NightlyRate != decimal.Truncate(input.NightlyRate))
            throw new BusinessException("Giá phòng phải là số nguyên VND lớn hơn 0.");

        // 2. Tải ảnh lên nếu người dùng có chọn ảnh mới
        var image = await _images.Upload(input.Image);

        // 3. Thực hiện lưu dữ liệu vào CSDL
        return await _transaction.RunAsync(async () =>
        {
            // Kiểm tra trùng số phòng
            if (await _db.Rooms.AnyAsync(x => x.RoomNumber == input.RoomNumber.Trim() && x.Id != input.Id))
                throw new BusinessException("Số phòng đã tồn tại.");

            var type = await _db.RoomTypes.FindAsync(input.RoomTypeId) ?? throw new BusinessException("Loại phòng không tồn tại.");
            var room = input.Id == 0 ? new Room() : await _db.Rooms.FindAsync(input.Id) ?? throw new BusinessException("Không tìm thấy phòng.");

            // Kiểm tra sức chứa mới có ảnh hưởng đơn đang đặt không
            if (await _db.Reservations.AnyAsync(x => x.RoomId == room.Id && (x.Status == ReservationStatus.Confirmed || x.Status == ReservationStatus.CheckedIn) && x.GuestCount > type.Capacity))
                throw new BusinessException("Loại phòng mới không đủ sức chứa cho các đơn đã xác nhận.");

            // Gán dữ liệu từ form vào đối tượng Room
            room.RoomNumber = input.RoomNumber.Trim();
            room.RoomTypeId = type.Id;
            room.NightlyRate = input.NightlyRate;
            room.Description = Sanitize(input.Description);
            room.Amenities = input.Amenities ?? "";
            if (image != null)
                room.ImagePath = image;

            if (input.Id == 0)
                _db.Rooms.Add(room);

            await _db.SaveChangesAsync();
            return room.Id;
        });
    }

    // Xóa phòng (chỉ cho phép xóa phòng chưa từng được đặt)
    public Task DeleteRoom(int id) => _transaction.RunAsync(async () =>
    {
        if (await _db.Reservations.AnyAsync(x => x.RoomId == id))
            throw new BusinessException("Phòng đã có lịch sử đặt. Hãy dùng trạng thái ngừng sử dụng.");

        var room = await _db.Rooms.FindAsync(id) ?? throw new BusinessException("Không tìm thấy phòng.");
        _db.Rooms.Remove(room);
    });

    // Thêm mới hoặc cập nhật loại phòng
    public Task SaveType(RoomTypeInput input) => _transaction.RunAsync(async () =>
    {
        var type = input.Id == 0 ? new RoomType() : await _db.RoomTypes.FindAsync(input.Id) ?? throw new BusinessException("Không tìm thấy loại phòng.");

        if (input.Id != 0 && await _db.Reservations.AnyAsync(x => x.Room.RoomTypeId == input.Id && (x.Status == ReservationStatus.Confirmed || x.Status == ReservationStatus.CheckedIn) && x.GuestCount > input.Capacity))
            throw new BusinessException("Sức chứa mới không phù hợp với đơn đã xác nhận.");

        type.Name = input.Name.Trim();
        type.Capacity = input.Capacity;
        type.Description = input.Description ?? "";

        if (input.Id == 0)
            _db.RoomTypes.Add(type);
    });

    // Xóa loại phòng
    public Task DeleteType(int id) => _transaction.RunAsync(async () =>
    {
        if (await _db.Rooms.AnyAsync(x => x.RoomTypeId == id))
            throw new BusinessException("Loại phòng đang được sử dụng. Không thể xóa.");

        var type = await _db.RoomTypes.FindAsync(id) ?? throw new BusinessException("Không tìm thấy loại phòng.");
        _db.RoomTypes.Remove(type);
    });

    // Thêm mới hoặc cập nhật dịch vụ
    public Task<int> SaveService(ServiceInput input) => _transaction.RunAsync(async () =>
    {
        if (input.UnitPrice < 0 || input.UnitPrice != decimal.Truncate(input.UnitPrice))
            throw new BusinessException("Đơn giá dịch vụ phải là số nguyên VND không âm.");

        var service = input.Id == 0 ? new HotelService() : await _db.Services.FindAsync(input.Id) ?? throw new BusinessException("Không tìm thấy dịch vụ.");
        service.Name = input.Name.Trim();
        service.Unit = input.Unit.Trim();
        service.UnitPrice = input.UnitPrice;
        service.IsActive = input.IsActive;

        if (input.Id == 0)
            _db.Services.Add(service);

        await _db.SaveChangesAsync();
        return service.Id;
    });

    // Xóa dịch vụ (nếu đã từng dùng thì chỉ đổi IsActive = false để lưu lịch sử)
    public Task DeleteService(int id) => _transaction.RunAsync(async () =>
    {
        var service = await _db.Services.FindAsync(id) ?? throw new BusinessException("Không tìm thấy dịch vụ.");
        if (await _db.FolioItems.AnyAsync(x => x.ServiceId == id))
            service.IsActive = false;
        else
            _db.Services.Remove(service);
    });

    // Lấy danh sách dịch vụ có tìm kiếm và phân trang
    public async Task<PageResult<HotelService>> Services(string? q, int page, bool activeOnly = false)
    {
        var query = _db.Services.AsNoTracking().Where(x => !activeOnly || x.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.Name.Contains(q));

        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (total + 11) / 12));
        var items = await query.OrderBy(x => x.Name).Skip((page - 1) * 12).Take(12).ToListAsync();

        return new(items, total, page, 12);
    }
}
