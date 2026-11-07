using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Areas.Staff.Controllers;

public class FrontDeskController(HotelDbContext db, FrontDeskService frontDesk, IHotelClock clock) : StaffController
{
    public async Task<IActionResult> Index(string tab = "arrivals")
    {
        tab = tab is "inhouse" or "departures" ? tab : "arrivals";
        var arrivals = await db.Reservations.CountAsync(x => x.Status == ReservationStatus.Confirmed && x.ArrivalDate == clock.Today);
        var overdue = await db.Reservations.CountAsync(x => x.Status == ReservationStatus.Confirmed && x.ArrivalDate < clock.Today);
        var inHouse = await db.Reservations.CountAsync(x => x.Status == ReservationStatus.CheckedIn);
        var departures = await db.Reservations.CountAsync(x => x.Status == ReservationStatus.CheckedIn && x.DepartureDate <= clock.Today);
        var query = db.Reservations.AsNoTracking().Include(x => x.Room).Include(x => x.GuestProfile).Include(x => x.Folio).AsQueryable();
        query = tab switch
        {
            "inhouse" => query.Where(x => x.Status == ReservationStatus.CheckedIn),
            "departures" => query.Where(x => x.Status == ReservationStatus.CheckedIn && x.DepartureDate <= clock.Today),
            _ => query.Where(x => x.Status == ReservationStatus.Confirmed && x.ArrivalDate <= clock.Today)};
        return View(new FrontDeskPage(tab, clock.Today, arrivals, overdue, inHouse, departures, await query.OrderBy(x => x.ArrivalDate).ToListAsync()));
    }

    [HttpPost]
    public async Task<IActionResult> CheckIn(int id)
    {
        try
        {
            var folio = await frontDesk.CheckIn(id);
            TempData["Success"] = "Đã nhận phòng và mở bảng kê.";
            return RedirectToAction(nameof(Folio), new { id = folio });
        }
        catch (BusinessException e)
        {
            TempData["Error"] = e.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> Folio(int id)
    {
        if (!await db.Folios.AnyAsync(x => x.Id == id))
            return NotFound();
        ViewBag.Services = await db.Services.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
        return View(await frontDesk.ReadFolio(id));
    }

    public async Task<IActionResult> Invoice(int id)
    {
        if (!await db.Folios.AnyAsync(x => x.Id == id))
            return NotFound();
        return View(await frontDesk.ReadFolio(id));
    }

    [HttpPost]
    public async Task<IActionResult> Charge(int id, ChargeInput input)
    {
        await Run(() => frontDesk.Charge(id, input), "Đã ghi dịch vụ vào bảng kê.");
        return RedirectToAction(nameof(Folio), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateCharge(int id, int itemId, int quantity)
    {
        await Run(() => frontDesk.UpdateCharge(id, itemId, quantity), "Đã cập nhật số lượng.");
        return RedirectToAction(nameof(Folio), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCharge(int id, int itemId)
    {
        await Run(() => frontDesk.UpdateCharge(id, itemId, 0, true), "Đã xóa khoản dịch vụ.");
        return RedirectToAction(nameof(Folio), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Pay(int id, PaymentInput input)
    {
        await Run(() => frontDesk.Pay(id, input, Actor), "Đã ghi nhận thanh toán đủ. Các khoản phí đã được khóa.");
        return RedirectToAction(nameof(Folio), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> CheckOut(int id)
    {
        await Run(() => frontDesk.CheckOut(id), "Đã trả phòng. Bảng kê đóng, phòng chuyển sang cần dọn.");
        return RedirectToAction(nameof(Folio), new { id });
    }
}
