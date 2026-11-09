using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Areas.Staff.Controllers;

/// <summary>
/// Controller quản lý đơn đặt phòng dành cho nhân viên Lễ tân và Quản trị viên:
/// Tìm kiếm, lọc đơn, tạo đơn tại quầy, cập nhật lịch, duyệt/xác nhận, hủy hoặc từ chối đơn.
/// </summary>
public class ReservationsController : StaffController
{
    private readonly HotelDbContext _db;
    private readonly ReservationService _reservations;
    private readonly IHotelClock _clock;

    public ReservationsController(HotelDbContext db, ReservationService reservations, IHotelClock clock)
    {
        _db = db;
        _reservations = reservations;
        _clock = clock;
    }

    // GET: /Staff/Reservations - Danh sách đơn đặt phòng với bộ lọc và phân trang
    public async Task<IActionResult> Index(string? q, ReservationStatus? status, DateOnly? from, DateOnly? to, int page = 1)
    {
        var query = _db.Reservations.AsNoTracking()
            .Include(x => x.Room)
            .Include(x => x.GuestProfile)
            .AsQueryable();

        // Tìm kiếm theo từ khóa (Mã đơn, Tên khách, Số phòng)
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.ConfirmationCode.Contains(q) || x.GuestProfile.FullName.Contains(q) || x.Room.RoomNumber.Contains(q));

        // Lọc theo trạng thái
        if (status.HasValue)
            query = query.Where(x => x.Status == status);

        // Lọc theo khoảng ngày đến
        if (from.HasValue)
            query = query.Where(x => x.ArrivalDate >= from);
        if (to.HasValue)
            query = query.Where(x => x.ArrivalDate <= to);

        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (total + 11) / 12));

        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * 12).Take(12).ToListAsync();
        return View(new PageResult<Reservation>(items, total, page, 12));
    }

    // GET: /Staff/Reservations/Details/{id} - Xem chi tiết một đơn đặt phòng
    public async Task<IActionResult> Details(int id)
    {
        var item = await _db.Reservations.AsNoTracking()
            .Include(x => x.Room).ThenInclude(x => x.RoomType)
            .Include(x => x.GuestProfile)
            .Include(x => x.Folio)
            .SingleOrDefaultAsync(x => x.Id == id);

        return item == null ? NotFound() : View(item);
    }

    // Nạp danh sách phòng và khách hàng vào ViewBag cho dropdown list
    private async Task Lists()
    {
        ViewBag.Rooms = await _db.Rooms.AsNoTracking().Include(x => x.RoomType).OrderBy(x => x.RoomNumber).ToListAsync();
        ViewBag.Guests = await _db.GuestProfiles.AsNoTracking().OrderBy(x => x.FullName).ToListAsync();
    }

    // GET: /Staff/Reservations/Create - Hiển thị form tạo đơn tại quầy
    public async Task<IActionResult> Create(int? guestId)
    {
        await Lists();
        return View(new BookingInput
        {
            GuestProfileId = guestId ?? 0,
            ArrivalDate = _clock.Today,
            DepartureDate = _clock.Today.AddDays(2)
        });
    }

    // POST: /Staff/Reservations/Create - Xử lý tạo đơn tại quầy
    [HttpPost]
    public async Task<IActionResult> Create(BookingInput input)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var id = await _reservations.Create(input, Actor);
                TempData["Success"] = "Đã tạo yêu cầu đặt phòng thành công. Hãy bấm Xác nhận để giữ chỗ!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (BusinessException e)
            {
                ModelState.AddModelError("", e.Message);
            }
        }

        await Lists();
        return View(input);
    }

    // GET: /Staff/Reservations/Edit/{id} - Hiển thị form chỉnh sửa lịch/phòng
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _db.Reservations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (item == null)
            return NotFound();

        await Lists();
        ViewBag.Id = id;
        return View(new StayInput
        {
            RoomId = item.RoomId,
            ArrivalDate = item.ArrivalDate,
            DepartureDate = item.DepartureDate,
            GuestCount = item.GuestCount,
            Notes = item.Notes
        });
    }

    // POST: /Staff/Reservations/Edit/{id} - Xử lý cập nhật thông tin đơn
    [HttpPost]
    public async Task<IActionResult> Edit(int id, StayInput input)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _reservations.Update(id, input);
                TempData["Success"] = "Đã cập nhật đơn đặt phòng thành công.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (BusinessException e)
            {
                ModelState.AddModelError("", e.Message);
            }
        }

        await Lists();
        ViewBag.Id = id;
        return View(input);
    }

    // POST: /Staff/Reservations/Confirm/{id} - Xác nhận / Duyệt đơn đặt phòng
    [HttpPost]
    public async Task<IActionResult> Confirm(int id)
    {
        await Run(() => _reservations.Confirm(id), "Đã xác nhận và chốt giá phòng.");
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Staff/Reservations/Cancel/{id} - Hủy đơn đặt phòng
    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        await Run(() => _reservations.Cancel(id), "Đã hủy đơn đặt phòng.");
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Staff/Reservations/Reject/{id} - Từ chối đơn đặt phòng
    [HttpPost]
    public async Task<IActionResult> Reject(int id)
    {
        await Run(() => _reservations.Cancel(id, reject: true), "Đã từ chối đơn đặt phòng.");
        return RedirectToAction(nameof(Details), new { id });
    }
}
