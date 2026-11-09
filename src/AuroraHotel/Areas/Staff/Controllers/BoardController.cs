using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Areas.Staff.Controllers;

/// <summary>
/// Controller hiển thị sơ đồ phòng (Room Board) và quản lý trạng thái buồng phòng:
/// Xem phòng sạch/bẩn, đang có khách hay trống; cập nhật dọn phòng và trạng thái bảo trì.
/// </summary>
public class BoardController : StaffController
{
    private readonly HotelDbContext _db;
    private readonly RoomStatusService _roomStatus;

    public BoardController(HotelDbContext db, RoomStatusService roomStatus)
    {
        _db = db;
        _roomStatus = roomStatus;
    }

    // GET: /Staff/Board - Hiển thị sơ đồ tình trạng tất cả các phòng
    public async Task<IActionResult> Index(int? type, string? state)
    {
        var query = _db.Rooms.AsNoTracking()
            .Include(x => x.RoomType)
            .Include(x => x.Reservations.Where(r => r.Status == ReservationStatus.CheckedIn))
            .AsQueryable();

        // Lọc theo loại phòng
        if (type.HasValue)
            query = query.Where(x => x.RoomTypeId == type);

        // Lọc theo trạng thái phòng
        query = state switch
        {
            "dirty" => query.Where(x => x.HousekeepingStatus == HousekeepingStatus.Dirty),
            "clean" => query.Where(x => x.HousekeepingStatus == HousekeepingStatus.Clean),
            "occupied" => query.Where(x => x.Reservations.Any(r => r.Status == ReservationStatus.CheckedIn)),
            "vacant" => query.Where(x => !x.Reservations.Any(r => r.Status == ReservationStatus.CheckedIn)),
            "inactive" => query.Where(x => x.OperationalStatus == OperationalStatus.OutOfOrder),
            _ => query
        };

        ViewBag.Types = await _db.RoomTypes.AsNoTracking().ToListAsync();
        return View(await query.OrderBy(x => x.RoomNumber).ToListAsync());
    }

    // POST: /Staff/Board/Housekeeping/{id} - Đổi trạng thái buồng phòng (Sạch / Cần dọn)
    [HttpPost]
    public async Task<IActionResult> Housekeeping(int id, HousekeepingStatus status)
    {
        await Run(() => _roomStatus.SetHousekeeping(id, status), "Đã cập nhật tình trạng dọn phòng thành công.");
        return RedirectToAction(nameof(Index));
    }

    // POST: /Staff/Board/Operation/{id} - Đổi trạng thái vận hành (Hoạt động / Ngừng khai thác - chỉ Admin)
    [Authorize(Roles = Roles.Admin), HttpPost]
    public async Task<IActionResult> Operation(int id, OperationalStatus status)
    {
        await Run(() => _roomStatus.SetOperation(id, status), "Đã cập nhật tình trạng khai thác thành công.");
        return RedirectToAction(nameof(Index));
    }
}
