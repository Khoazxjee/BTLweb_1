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

/// <summary>
/// Controller xử lý các chức năng đặt phòng của khách hàng trên website:
/// Chọn phòng, tạo đơn đặt phòng, xem danh sách đơn của mình và hủy đơn.
/// </summary>
public class BookingsController : Controller
{
    private readonly HotelDbContext _db;
    private readonly ReservationService _reservations;
    private readonly RoomAvailabilityService _availability;

    public BookingsController(HotelDbContext db, ReservationService reservations, RoomAvailabilityService availability)
    {
        _db = db;
        _reservations = reservations;
        _availability = availability;
    }

    // Lấy UserId của khách hàng đang đăng nhập từ Claims
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // POST: /Bookings/Select - Lưu thông tin phòng và ngày chọn vào Session
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
            _availability.ValidateStay(input.ArrivalDate, input.DepartureDate, input.GuestCount, 20);
        }
        catch (BusinessException e)
        {
            TempData["Error"] = e.Message;
            return RedirectToAction("Index", "Rooms");
        }

        // Lưu lựa chọn vào Session để giữ lại sau khi khách đăng nhập
        HttpContext.Session.SetString("BookingSelection", JsonSerializer.Serialize(input));

        // Nếu chưa đăng nhập thì chuyển đến trang Đăng nhập
        return User.Identity?.IsAuthenticated == true
            ? RedirectToAction(nameof(Create))
            : RedirectToAction("Login", "Account", new { returnUrl = "/Bookings/Create" });
    }

    // GET: /Bookings/Create - Hiển thị form xác nhận thông tin đặt phòng
    [Authorize(Roles = Roles.Customer), HttpGet]
    public async Task<IActionResult> Create()
    {
        var selection = HttpContext.Session.GetString("BookingSelection");
        if (selection == null)
        {
            TempData["Error"] = "Hãy chọn phòng và ngày ở trước.";
            return RedirectToAction("Index", "Rooms");
        }

        var stayInput = JsonSerializer.Deserialize<StayInput>(selection)!;
        return await BookingView(stayInput);
    }

    // Hàm phụ trợ nạp thông tin phòng và hồ sơ khách ra giao diện đặt phòng
    private async Task<IActionResult> BookingView(StayInput input)
    {
        var room = await _db.Rooms.AsNoTracking().Include(x => x.RoomType).SingleOrDefaultAsync(x => x.Id == input.RoomId);
        if (room == null)
            return NotFound();

        var guest = await _db.GuestProfiles.AsNoTracking().SingleAsync(x => x.UserId == UserId);
        return View("Create", new BookingPage(input, room, guest));
    }

    // POST: /Bookings/Create - Xử lý gửi yêu cầu đặt phòng (Pending)
    [Authorize(Roles = Roles.Customer), HttpPost]
    public async Task<IActionResult> Create(StayInput input, string? intent = null)
    {
        if (!ModelState.IsValid)
            return await BookingView(input);

        try
        {
            // Nếu người dùng chỉ muốn xem trước giá tiền khi đổi ngày
            if (intent == "preview")
            {
                await _availability.AvailableRoom(input);
                ViewData["Previewed"] = true;
                return await BookingView(input);
            }

            // Lấy ID hồ sơ của khách hàng
            var guestId = await _db.GuestProfiles.Where(x => x.UserId == UserId).Select(x => x.Id).SingleAsync();

            // Tạo đơn đặt phòng
            var id = await _reservations.Create(new BookingInput
            {
                RoomId = input.RoomId,
                GuestProfileId = guestId,
                ArrivalDate = input.ArrivalDate,
                DepartureDate = input.DepartureDate,
                GuestCount = input.GuestCount,
                Notes = input.Notes
            }, UserId, UserId);

            // Xóa thông tin tạm trong Session sau khi đặt thành công
            HttpContext.Session.Remove("BookingSelection");
            TempData["Success"] = "Đang chờ xác nhận. Yêu cầu của bạn đã được gửi tới khách sạn!";

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessException e)
        {
            ModelState.AddModelError("", e.Message);
            return await BookingView(input);
        }
    }

    // GET: /Bookings - Danh sách các đơn đặt phòng của khách hàng
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Index(int page = 1)
    {
        var query = _db.Reservations.AsNoTracking()
            .Include(x => x.Room).ThenInclude(x => x.RoomType)
            .Where(x => x.GuestProfile.UserId == UserId);

        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (total + 9) / 10));

        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * 10).Take(10).ToListAsync();
        return View(new PageResult<Reservation>(items, total, page, 10));
    }

    // GET: /Bookings/Details/{id} - Xem chi tiết đơn đặt phòng
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Details(int id)
    {
        var booking = await _db.Reservations.AsNoTracking()
            .Include(x => x.Room).ThenInclude(x => x.RoomType)
            .Include(x => x.GuestProfile)
            .SingleOrDefaultAsync(x => x.Id == id && x.GuestProfile.UserId == UserId);

        return booking == null ? NotFound() : View(booking);
    }

    // POST: /Bookings/Cancel/{id} - Khách hàng tự hủy đơn
    [Authorize(Roles = Roles.Customer), HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!await _db.Reservations.AnyAsync(x => x.Id == id && x.GuestProfile.UserId == UserId))
            return NotFound();

        try
        {
            await _reservations.Cancel(id, UserId);
            TempData["Success"] = "Đã hủy đơn đặt phòng thành công.";
        }
        catch (BusinessException e)
        {
            TempData["Error"] = e.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
