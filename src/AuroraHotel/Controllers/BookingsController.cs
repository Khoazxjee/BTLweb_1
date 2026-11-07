using System.Security.Claims;
using System.Text.Json;
using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Controllers;

public class BookingsController(HotelDbContext db, ReservationService reservations, RoomAvailabilityService availability) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost]
    public IActionResult Select(StayInput input)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Thông tin kỳ ở không hợp lệ.";
            return RedirectToAction("Index", "Rooms");
        }

        try
        {
            availability.ValidateStay(input.ArrivalDate, input.DepartureDate, input.GuestCount, 20);
        }
        catch (BusinessException e)
        {
            TempData["Error"] = e.Message;
            return RedirectToAction("Index", "Rooms");
        }

        HttpContext.Session.SetString("BookingSelection", JsonSerializer.Serialize(input));
        return User.Identity?.IsAuthenticated == true ? RedirectToAction(nameof(Create)) : RedirectToAction("Login", "Account", new { returnUrl = "/Bookings/Create" });
    }

    [Authorize(Roles = Roles.Customer), HttpGet]
    public async Task<IActionResult> Create()
    {
        var selection = HttpContext.Session.GetString("BookingSelection");
        if (selection == null)
        {
            TempData["Error"] = "Hãy chọn phòng và ngày ở trước.";
            return RedirectToAction("Index", "Rooms");
        }

        return await BookingView(JsonSerializer.Deserialize<StayInput>(selection)!);
    }

    private async Task<IActionResult> BookingView(StayInput input)
    {
        var room = await db.Rooms.AsNoTracking().Include(x => x.RoomType).SingleOrDefaultAsync(x => x.Id == input.RoomId);
        if (room == null)
            return NotFound();
        var guest = await db.GuestProfiles.AsNoTracking().SingleAsync(x => x.UserId == UserId);
        return View("Create", new BookingPage(input, room, guest));
    }

    [Authorize(Roles = Roles.Customer), HttpPost]
    public async Task<IActionResult> Create(StayInput input, string? intent = null)
    {
        if (!ModelState.IsValid)
            return await BookingView(input);
        try
        {
            if (intent == "preview")
            {
                await availability.AvailableRoom(input);
                ViewData["Previewed"] = true;
                return await BookingView(input);
            }

            var guestId = await db.GuestProfiles.Where(x => x.UserId == UserId).Select(x => x.Id).SingleAsync();
            var id = await reservations.Create(new BookingInput { RoomId = input.RoomId, GuestProfileId = guestId, ArrivalDate = input.ArrivalDate, DepartureDate = input.DepartureDate, GuestCount = input.GuestCount, Notes = input.Notes }, UserId, UserId);
            HttpContext.Session.Remove("BookingSelection");
            TempData["Success"] = "Đang chờ xác nhận. Yêu cầu của bạn đã được gửi; phòng chưa được giữ chỗ.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessException e)
        {
            ModelState.AddModelError("", e.Message);
            return await BookingView(input);
        }
    }

    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Index(int page = 1)
    {
        var query = db.Reservations.AsNoTracking().Include(x => x.Room).ThenInclude(x => x.RoomType).Where(x => x.GuestProfile.UserId == UserId);
        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (total + 9) / 10));
        return View(new PageResult<Reservation>(await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * 10).Take(10).ToListAsync(), total, page, 10));
    }

    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Details(int id)
    {
        var booking = await db.Reservations.AsNoTracking().Include(x => x.Room).ThenInclude(x => x.RoomType).Include(x => x.GuestProfile).SingleOrDefaultAsync(x => x.Id == id && x.GuestProfile.UserId == UserId);
        return booking == null ? NotFound() : View(booking);
    }

    [Authorize(Roles = Roles.Customer), HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!await db.Reservations.AnyAsync(x => x.Id == id && x.GuestProfile.UserId == UserId))
            return NotFound();
        try
        {
            await reservations.Cancel(id, UserId);
            TempData["Success"] = "Đã hủy đơn đặt phòng.";
        }
        catch (BusinessException e)
        {
            TempData["Error"] = e.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
