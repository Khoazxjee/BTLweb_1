using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Services;

/// <summary>
/// Service quản lý tình trạng phòng: Tình trạng dọn phòng (Sạch/Cần dọn)
/// và tình trạng vận hành (Hoạt động/Tạm dừng sửa chữa).
/// </summary>
public class RoomStatusService
{
    private readonly HotelDbContext _db;
    private readonly HotelTransaction _transaction;

    public RoomStatusService(HotelDbContext db, HotelTransaction transaction)
    {
        _db = db;
        _transaction = transaction;
    }

    // Cập nhật tình trạng dọn dẹp buồng phòng (Clean / Dirty)
    public Task SetHousekeeping(int roomId, HousekeepingStatus value) => _transaction.RunAsync(async () =>
    {
        if (!Enum.IsDefined(value))
            throw new BusinessException("Tình trạng dọn phòng không hợp lệ.");

        var room = await _db.Rooms.FindAsync(roomId)
            ?? throw new BusinessException("Không tìm thấy phòng.");

        room.HousekeepingStatus = value;
    });

    // Cập nhật trạng thái khai thác phòng (Active / OutOfOrder)
    public Task SetOperation(int roomId, OperationalStatus value) => _transaction.RunAsync(async () =>
    {
        if (!Enum.IsDefined(value))
            throw new BusinessException("Tình trạng khai thác không hợp lệ.");

        var room = await _db.Rooms.FindAsync(roomId)
            ?? throw new BusinessException("Không tìm thấy phòng.");

        // Nếu muốn chuyển sang Ngừng sử dụng, kiểm tra xem phòng có khách đang ở hoặc đơn đã duyệt không
        if (value == OperationalStatus.OutOfOrder && await _db.Reservations.AnyAsync(x =>
            x.RoomId == roomId &&
            (x.Status == ReservationStatus.Confirmed || x.Status == ReservationStatus.CheckedIn)))
        {
            throw new BusinessException("Phòng còn đơn đã xác nhận hoặc có khách. Hãy xử lý các đơn trước khi ngừng sử dụng.");
        }

        room.OperationalStatus = value;
    });
}
