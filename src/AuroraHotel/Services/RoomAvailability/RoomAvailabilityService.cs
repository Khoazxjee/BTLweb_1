using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Services;

/// <summary>
/// Service kiểm tra tính khả dụng của phòng và tìm kiếm phòng trống theo ngày.
/// </summary>
public class RoomAvailabilityService
{
    private readonly HotelDbContext _db;
    private readonly IHotelClock _clock;

    public RoomAvailabilityService(HotelDbContext db, IHotelClock clock)
    {
        _db = db;
        _clock = clock;
    }

    // Kiểm tra tính hợp lệ của khoảng thời gian lưu trú và số lượng khách
    public void ValidateStay(DateOnly arrival, DateOnly departure, int guests, int capacity)
    {
        // 1. Ngày đến không được nhỏ hơn ngày hôm nay
        if (arrival < _clock.Today)
            throw new BusinessException("Ngày nhận phòng không được ở trong quá khứ.");

        // 2. Ngày đi phải sau ngày đến
        if (departure <= arrival)
            throw new BusinessException("Ngày trả phòng phải sau ngày nhận phòng.");

        // 3. Giới hạn thời gian lưu trú tối đa 365 ngày
        if (departure.DayNumber - arrival.DayNumber > 365)
            throw new BusinessException("Mỗi đơn hỗ trợ tối đa 365 đêm.");

        // 4. Số khách không được vượt quá sức chứa của phòng
        if (guests < 1 || guests > capacity)
            throw new BusinessException($"Phòng phục vụ tối đa {capacity} khách.");
    }

    // Kiểm tra xem một phòng cụ thể có khả dụng trong khoảng ngày chỉ định không
    public async Task<Room> AvailableRoom(StayInput input, int? except = null)
    {
        // Lấy thông tin phòng kèm loại phòng
        var room = await _db.Rooms.Include(x => x.RoomType).SingleOrDefaultAsync(x => x.Id == input.RoomId)
            ?? throw new BusinessException("Không tìm thấy phòng.");

        // Kiểm tra tính hợp lệ về ngày và số khách
        ValidateStay(input.ArrivalDate, input.DepartureDate, input.GuestCount, room.RoomType.Capacity);

        // Kiểm tra phòng có đang mở cửa hoạt động không
        if (room.OperationalStatus != OperationalStatus.Active)
            throw new BusinessException("Phòng đang ngừng sử dụng. Vui lòng chọn phòng khác.");

        // Kiểm tra trùng lịch với các đơn đã Confirmed hoặc CheckedIn
        var isOverlap = await _db.Reservations.AnyAsync(x =>
            x.RoomId == room.Id &&
            x.Id != except &&
            (x.Status == ReservationStatus.Confirmed || x.Status == ReservationStatus.CheckedIn) &&
            input.ArrivalDate < x.DepartureDate && input.DepartureDate > x.ArrivalDate);

        if (isOverlap)
            throw new BusinessException("Phòng vừa được xác nhận cho kỳ ở trùng lịch. Vui lòng chọn phòng hoặc ngày khác.");

        return room;
    }

    // Tìm kiếm và lọc danh sách phòng trống trên website
    public async Task<PageResult<Room>> Search(SearchInput input)
    {
        // Thiết lập ngày mặc định nếu người dùng chưa chọn
        input.ArrivalDate ??= _clock.Today;
        var remainingDays = DateOnly.MaxValue.DayNumber - input.ArrivalDate.Value.DayNumber;
        input.DepartureDate ??= input.ArrivalDate.Value.AddDays(Math.Min(2, remainingDays));

        ValidateStay(input.ArrivalDate.Value, input.DepartureDate.Value, input.GuestCount, 20);

        if (input.MinPrice < 0 || input.MaxPrice < 0 || input.MinPrice > input.MaxPrice)
            throw new BusinessException("Khoảng giá chưa hợp lệ.");

        // Xây dựng câu truy vấn LINQ: Phòng đang hoạt động, đủ sức chứa và không trùng lịch
        var query = _db.Rooms.AsNoTracking()
            .Include(x => x.RoomType)
            .Where(x => x.OperationalStatus == OperationalStatus.Active && x.RoomType.Capacity >= input.GuestCount)
            .Where(x => !x.Reservations.Any(r =>
                (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.CheckedIn) &&
                input.ArrivalDate < r.DepartureDate && input.DepartureDate > r.ArrivalDate));

        // Lọc theo loại phòng nếu có chọn
        if (input.RoomTypeId.HasValue)
            query = query.Where(x => x.RoomTypeId == input.RoomTypeId);

        // Lọc theo khoảng giá
        if (input.MinPrice.HasValue)
            query = query.Where(x => x.NightlyRate >= input.MinPrice);
        if (input.MaxPrice.HasValue)
            query = query.Where(x => x.NightlyRate <= input.MaxPrice);

        // Đếm tổng số phòng thỏa mãn điều kiện
        var count = await query.CountAsync();

        // Sắp xếp kết quả
        query = input.Sort switch
        {
            "price-desc" => query.OrderByDescending(x => x.NightlyRate).ThenBy(x => x.Id),
            "capacity" => query.OrderByDescending(x => x.RoomType.Capacity).ThenBy(x => x.Id),
            _ => query.OrderBy(x => x.NightlyRate).ThenBy(x => x.Id)
        };

        // Phân trang kết quả (mỗi trang 9 phòng)
        input.Page = Math.Clamp(input.Page, 1, Math.Max(1, (count + 8) / 9));
        var items = await query.Skip((input.Page - 1) * 9).Take(9).ToListAsync();

        return new(items, count, input.Page);
    }
}
