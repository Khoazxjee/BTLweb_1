using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Services;

public class RoomStatusService(HotelDbContext db, HotelTransaction transaction)
{
    public Task SetHousekeeping(int roomId, HousekeepingStatus value) => transaction.RunAsync(async () =>
    {
        if (!Enum.IsDefined(value))
            throw new BusinessException("Tình trạng dọn phòng không hợp lệ.");
        var room = await db.Rooms.FindAsync(roomId) ?? throw new BusinessException("Không tìm thấy phòng.");
        room.HousekeepingStatus = value;
    });
    public Task SetOperation(int roomId, OperationalStatus value) => transaction.RunAsync(async () =>
    {
        if (!Enum.IsDefined(value))
            throw new BusinessException("Tình trạng khai thác không hợp lệ.");
        var room = await db.Rooms.FindAsync(roomId) ?? throw new BusinessException("Không tìm thấy phòng.");
        if (value == OperationalStatus.OutOfOrder && await db.Reservations.AnyAsync(x => x.RoomId == roomId && (x.Status == ReservationStatus.Confirmed || x.Status == ReservationStatus.CheckedIn)))
            throw new BusinessException("Phòng còn đơn đã xác nhận hoặc có khách. Hãy xử lý các đơn trước khi ngừng sử dụng.");
        room.OperationalStatus = value;
    });
}
