using AuroraHotel.Data;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Controllers;

/// <summary>
/// Controller hiển thị danh sách phòng, tìm kiếm lọc phòng và xem chi tiết phòng dành cho khách truy cập.
/// </summary>
public class RoomsController : Controller
{
    private readonly HotelDbContext _db;
    private readonly RoomAvailabilityService _availability;
    private readonly IHotelClock _clock;

    public RoomsController(HotelDbContext db, RoomAvailabilityService availability, IHotelClock clock)
    {
        _db = db;
        _availability = availability;
        _clock = clock;
    }

    // GET: /Rooms - Trang tìm kiếm và danh sách phòng (hỗ trợ cả request thường và AJAX)
    public async Task<IActionResult> Index(SearchInput input)
    {
        Response.Headers.Append("Vary", "X-Requested-With");
        PageResult<Models.Room> result = new(new List<Models.Room>(), 0, 1);

        try
        {
            if (ModelState.IsValid)
            {
                result = await _availability.Search(input);
            }
            else
            {
                ModelState.AddModelError("", "Vui lòng kiểm tra các tiêu chí tìm kiếm.");
            }
        }
        catch (BusinessException e)
        {
            ModelState.AddModelError("", e.Message);
        }

        var roomTypes = await _db.RoomTypes.AsNoTracking().ToListAsync();
        var page = new SearchPage(input, result, roomTypes);

        // Nếu là yêu cầu AJAX từ JavaScript, chỉ trả về partial view chứa danh sách kết quả
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("_Results", page);
        }

        // Ngược lại trả về toàn bộ trang HTML
        return View(page);
    }

    // GET: /Rooms/Details/{id} - Xem chi tiết một phòng cụ thể
    public async Task<IActionResult> Details(int id, DateOnly? arrivalDate, DateOnly? departureDate, int guestCount = 2, string? returnUrl = null)
    {
        // 1. Tìm thông tin phòng
        var room = await _db.Rooms.AsNoTracking()
            .Include(x => x.RoomType)
            .SingleOrDefaultAsync(x => x.Id == id && x.OperationalStatus == Models.OperationalStatus.Active);

        if (room == null)
            return NotFound();

        // 2. Khởi tạo ngày mặc định
        var searchReturnUrl = returnUrl != null && Url.IsLocalUrl(returnUrl) && returnUrl.StartsWith("/Rooms", StringComparison.OrdinalIgnoreCase) ? returnUrl : null;
        var arrival = arrivalDate ?? _clock.Today;
        var stay = new StayInput
        {
            RoomId = id,
            ArrivalDate = arrival,
            DepartureDate = departureDate ?? arrival.AddDays(Math.Min(2, DateOnly.MaxValue.DayNumber - arrival.DayNumber)),
            GuestCount = guestCount
        };

        // 3. Kiểm tra tính hợp lệ
        try
        {
            if (!ModelState.IsValid)
                throw new BusinessException("Thông tin ngày ở hoặc số khách không hợp lệ.");

            _availability.ValidateStay(stay.ArrivalDate, stay.DepartureDate, stay.GuestCount, room.RoomType.Capacity);
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError("", exception.Message);
        }

        return View(new RoomDetailsPage(room, stay, searchReturnUrl));
    }
}
