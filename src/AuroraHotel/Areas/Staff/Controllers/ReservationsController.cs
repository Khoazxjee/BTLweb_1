using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Areas.Staff.Controllers;

public class ReservationsController(HotelDbContext db, ReservationService reservations, IHotelClock clock) : StaffController
{
    public async Task<IActionResult> Index(string? q, ReservationStatus? status, DateOnly? from, DateOnly? to, int page = 1)
    {
        var query = db.Reservations.AsNoTracking().Include(x => x.Room).Include(x => x.GuestProfile).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.ConfirmationCode.Contains(q) || x.GuestProfile.FullName.Contains(q) || x.Room.RoomNumber.Contains(q));
        if (status.HasValue)
            query = query.Where(x => x.Status == status);
        if (from.HasValue)
            query = query.Where(x => x.ArrivalDate >= from);
        if (to.HasValue)
            query = query.Where(x => x.ArrivalDate <= to);
        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (total + 11) / 12));
        return View(new PageResult<Reservation>(await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * 12).Take(12).ToListAsync(), total, page, 12));
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await db.Reservations.AsNoTracking().Include(x => x.Room).ThenInclude(x => x.RoomType).Include(x => x.GuestProfile).Include(x => x.Folio).SingleOrDefaultAsync(x => x.Id == id);
        return item == null ? NotFound() : View(item);
    }

    private async Task Lists()
    {
        ViewBag.Rooms = await db.Rooms.AsNoTracking().Include(x => x.RoomType).OrderBy(x => x.RoomNumber).ToListAsync();
        ViewBag.Guests = await db.GuestProfiles.AsNoTracking().OrderBy(x => x.FullName).ToListAsync();
    }

    public async Task<IActionResult> Create(int? guestId)
    {
        await Lists();
        return View(new BookingInput { GuestProfileId = guestId ?? 0, ArrivalDate = clock.Today, DepartureDate = clock.Today.AddDays(2) });
    }

    [HttpPost]
    public async Task<IActionResult> Create(BookingInput input)
    {
        if (ModelState.IsValid)
            try
            {
                var id = await reservations.Create(input, Actor);
                TempData["Success"] = "Đã tạo yêu cầu. Hãy xác nhận để giữ chỗ.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (BusinessException e)
            {
                ModelState.AddModelError("", e.Message);
            }

        await Lists();
        return View(input);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.Reservations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (item == null)
            return NotFound();
        await Lists();
        ViewBag.Id = id;
        return View(new StayInput { RoomId = item.RoomId, ArrivalDate = item.ArrivalDate, DepartureDate = item.DepartureDate, GuestCount = item.GuestCount, Notes = item.Notes });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, StayInput input)
    {
        if (ModelState.IsValid)
            try
            {
                await reservations.Update(id, input);
                TempData["Success"] = "Đã cập nhật đơn và kiểm tra khả dụng.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (BusinessException e)
            {
                ModelState.AddModelError("", e.Message);
            }

        await Lists();
        ViewBag.Id = id;
        return View(input);
    }

    [HttpPost]
    public async Task<IActionResult> Confirm(int id)
    {
        await Run(() => reservations.Confirm(id), "Đã xác nhận và chốt giá phòng.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        await Run(() => reservations.Cancel(id), "Đã hủy đơn.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Reject(int id)
    {
        await Run(() => reservations.Cancel(id, reject: true), "Đã từ chối đơn.");
        return RedirectToAction(nameof(Details), new { id });
    }
}
