using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Services;

public class RoomAvailabilityService(HotelDbContext db, IHotelClock clock)
{
    public void ValidateStay(DateOnly arrival, DateOnly departure, int guests, int capacity)
    {
        if (arrival < clock.Today)
            throw new BusinessException("Ngày nhận phòng không được ở trong quá khứ.");
        if (departure <= arrival)
            throw new BusinessException("Ngày trả phòng phải sau ngày nhận phòng.");
        if (departure.DayNumber - arrival.DayNumber > 365)
            throw new BusinessException("Mỗi đơn hỗ trợ tối đa 365 đêm.");
        if (guests < 1 || guests > capacity)
            throw new BusinessException($"Phòng phục vụ tối đa {capacity} khách.");
    }

    public async Task<Room> AvailableRoom(StayInput input, int? except = null)
    {
        var room = await db.Rooms.Include(x => x.RoomType).SingleOrDefaultAsync(x => x.Id == input.RoomId) ?? throw new BusinessException("Không tìm thấy phòng.");
        ValidateStay(input.ArrivalDate, input.DepartureDate, input.GuestCount, room.RoomType.Capacity);
        if (room.OperationalStatus != OperationalStatus.Active)
            throw new BusinessException("Phòng đang ngừng sử dụng. Vui lòng chọn phòng khác.");
        if (await db.Reservations.AnyAsync(x => x.RoomId == room.Id && x.Id != except && (x.Status == ReservationStatus.Confirmed || x.Status == ReservationStatus.CheckedIn) && input.ArrivalDate < x.DepartureDate && input.DepartureDate > x.ArrivalDate))
            throw new BusinessException("Phòng vừa được xác nhận cho kỳ ở trùng lịch. Vui lòng chọn phòng hoặc ngày khác.");
        return room;
    }

    public async Task<PageResult<Room>> Search(SearchInput input)
    {
        input.ArrivalDate ??= clock.Today;
        var remainingDays = DateOnly.MaxValue.DayNumber - input.ArrivalDate.Value.DayNumber;
        input.DepartureDate ??= input.ArrivalDate.Value.AddDays(Math.Min(2, remainingDays));
        ValidateStay(input.ArrivalDate.Value, input.DepartureDate.Value, input.GuestCount, 20);
        if (input.MinPrice < 0 || input.MaxPrice < 0 || input.MinPrice > input.MaxPrice)
            throw new BusinessException("Khoảng giá chưa hợp lệ.");
        var query = db.Rooms.AsNoTracking().Include(x => x.RoomType).Where(x => x.OperationalStatus == OperationalStatus.Active && x.RoomType.Capacity >= input.GuestCount).Where(x => !x.Reservations.Any(r => (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.CheckedIn) && input.ArrivalDate < r.DepartureDate && input.DepartureDate > r.ArrivalDate));
        if (input.RoomTypeId.HasValue)
            query = query.Where(x => x.RoomTypeId == input.RoomTypeId);
        if (input.MinPrice.HasValue)
            query = query.Where(x => x.NightlyRate >= input.MinPrice);
        if (input.MaxPrice.HasValue)
            query = query.Where(x => x.NightlyRate <= input.MaxPrice);
        var count = await query.CountAsync();
        query = input.Sort switch
        {
            "price-desc" => query.OrderByDescending(x => x.NightlyRate).ThenBy(x => x.Id),
            "capacity" => query.OrderByDescending(x => x.RoomType.Capacity).ThenBy(x => x.Id),
            _ => query.OrderBy(x => x.NightlyRate).ThenBy(x => x.Id)};
        input.Page = Math.Clamp(input.Page, 1, Math.Max(1, (count + 8) / 9));
        return new(await query.Skip((input.Page - 1) * 9).Take(9).ToListAsync(), count, input.Page);
    }
}
