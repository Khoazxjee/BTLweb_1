using AuroraHotel.Data;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Controllers;

public class RoomsController(HotelDbContext db, RoomAvailabilityService availability, IHotelClock clock) : Controller
{
    public async Task<IActionResult> Index(SearchInput input)
    {
        Response.Headers.Append("Vary", "X-Requested-With");
        PageResult<Models.Room> result = new([], 0, 1);
        try
        {
            if (ModelState.IsValid)
                result = await availability.Search(input);
            else
                ModelState.AddModelError("", "Vui lòng kiểm tra các tiêu chí tìm kiếm.");
        }
        catch (BusinessException e)
        {
            ModelState.AddModelError("", e.Message);
        }

        var page = new SearchPage(input, result, await db.RoomTypes.AsNoTracking().ToListAsync());
        return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ? PartialView("_Results", page) : View(page);
    }

    public async Task<IActionResult> Details(int id, DateOnly? arrivalDate, DateOnly? departureDate, int guestCount = 2, string? returnUrl = null)
    {
        var room = await db.Rooms.AsNoTracking().Include(x => x.RoomType).SingleOrDefaultAsync(x => x.Id == id && x.OperationalStatus == Models.OperationalStatus.Active);
        if (room == null)
            return NotFound();
        var searchReturnUrl = returnUrl != null && Url.IsLocalUrl(returnUrl) && returnUrl.StartsWith("/Rooms", StringComparison.OrdinalIgnoreCase) ? returnUrl : null;
        var arrival = arrivalDate ?? clock.Today;
        var stay = new StayInput
        {
            RoomId = id,
            ArrivalDate = arrival,
            DepartureDate = departureDate ?? arrival.AddDays(Math.Min(2, DateOnly.MaxValue.DayNumber - arrival.DayNumber)),
            GuestCount = guestCount
        };
        try
        {
            if (!ModelState.IsValid)
                throw new BusinessException("Thông tin ngày ở hoặc số khách không hợp lệ.");
            availability.ValidateStay(stay.ArrivalDate, stay.DepartureDate, stay.GuestCount, room.RoomType.Capacity);
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError("", exception.Message);
        }

        return View(new RoomDetailsPage(room, stay, searchReturnUrl));
    }
}
