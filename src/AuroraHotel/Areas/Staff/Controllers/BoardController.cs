using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Areas.Staff.Controllers;

public class BoardController(HotelDbContext db, RoomStatusService roomStatus) : StaffController
{
    public async Task<IActionResult> Index(int? type, string? state)
    {
        var query = db.Rooms.AsNoTracking().Include(x => x.RoomType).Include(x => x.Reservations.Where(r => r.Status == ReservationStatus.CheckedIn)).AsQueryable();
        if (type.HasValue)
            query = query.Where(x => x.RoomTypeId == type);
        query = state switch
        {
            "dirty" => query.Where(x => x.HousekeepingStatus == HousekeepingStatus.Dirty),
            "clean" => query.Where(x => x.HousekeepingStatus == HousekeepingStatus.Clean),
            "occupied" => query.Where(x => x.Reservations.Any(r => r.Status == ReservationStatus.CheckedIn)),
            "vacant" => query.Where(x => !x.Reservations.Any(r => r.Status == ReservationStatus.CheckedIn)),
            "inactive" => query.Where(x => x.OperationalStatus == OperationalStatus.OutOfOrder),
            _ => query
        };
        ViewBag.Types = await db.RoomTypes.AsNoTracking().ToListAsync();
        return View(await query.OrderBy(x => x.RoomNumber).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Housekeeping(int id, HousekeepingStatus status)
    {
        await Run(() => roomStatus.SetHousekeeping(id, status), "Đã cập nhật tình trạng dọn phòng.");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.Admin), HttpPost]
    public async Task<IActionResult> Operation(int id, OperationalStatus status)
    {
        await Run(() => roomStatus.SetOperation(id, status), "Đã cập nhật tình trạng khai thác.");
        return RedirectToAction(nameof(Index));
    }
}
