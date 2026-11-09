using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Areas.Staff.Controllers;

/// <summary>
/// Controller màn hình làm việc của Lễ tân:
/// Quản lý danh sách khách đến hôm nay, khách đang ở, khách sắp trả phòng;
/// Làm thủ tục Check-in, gọi dịch vụ, thu tiền và Check-out.
/// </summary>
public class FrontDeskController : StaffController
{
    private readonly HotelDbContext _db;
    private readonly FrontDeskService _frontDesk;
    private readonly IHotelClock _clock;

    public FrontDeskController(HotelDbContext db, FrontDeskService frontDesk, IHotelClock clock)
    {
        _db = db;
        _frontDesk = frontDesk;
        _clock = clock;
    }

    // GET: /Staff/FrontDesk - Bảng điều khiển nghiệp vụ Lễ tân
    public async Task<IActionResult> Index(string tab = "arrivals")
    {
        tab = tab is "inhouse" or "departures" ? tab : "arrivals";

        // Thống kê số lượng các đơn trong ngày
        var arrivals = await _db.Reservations.CountAsync(x => x.Status == ReservationStatus.Confirmed && x.ArrivalDate == _clock.Today);
        var overdue = await _db.Reservations.CountAsync(x => x.Status == ReservationStatus.Confirmed && x.ArrivalDate < _clock.Today);
        var inHouse = await _db.Reservations.CountAsync(x => x.Status == ReservationStatus.CheckedIn);
        var departures = await _db.Reservations.CountAsync(x => x.Status == ReservationStatus.CheckedIn && x.DepartureDate <= _clock.Today);

        var query = _db.Reservations.AsNoTracking()
            .Include(x => x.Room)
            .Include(x => x.GuestProfile)
            .Include(x => x.Folio)
            .AsQueryable();

        // Lọc theo từng tab giao diện
        query = tab switch
        {
            "inhouse" => query.Where(x => x.Status == ReservationStatus.CheckedIn),
            "departures" => query.Where(x => x.Status == ReservationStatus.CheckedIn && x.DepartureDate <= _clock.Today),
            _ => query.Where(x => x.Status == ReservationStatus.Confirmed && x.ArrivalDate <= _clock.Today)
        };

        var items = await query.OrderBy(x => x.ArrivalDate).ToListAsync();
        return View(new FrontDeskPage(tab, _clock.Today, arrivals, overdue, inHouse, departures, items));
    }

    // POST: /Staff/FrontDesk/CheckIn/{id} - Làm thủ tục nhận phòng
    [HttpPost]
    public async Task<IActionResult> CheckIn(int id)
    {
        try
        {
            var folio = await _frontDesk.CheckIn(id);
            TempData["Success"] = "Đã nhận phòng và mở bảng kê thanh toán thành công.";
            return RedirectToAction(nameof(Folio), new { id = folio });
        }
        catch (BusinessException e)
        {
            TempData["Error"] = e.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: /Staff/FrontDesk/Folio/{id} - Xem chi tiết bảng kê thanh toán
    public async Task<IActionResult> Folio(int id)
    {
        if (!await _db.Folios.AnyAsync(x => x.Id == id))
            return NotFound();

        ViewBag.Services = await _db.Services.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
        return View(await _frontDesk.ReadFolio(id));
    }

    // GET: /Staff/FrontDesk/Invoice/{id} - Xem hóa đơn thanh toán để in
    public async Task<IActionResult> Invoice(int id)
    {
        if (!await _db.Folios.AnyAsync(x => x.Id == id))
            return NotFound();

        return View(await _frontDesk.ReadFolio(id));
    }

    // POST: /Staff/FrontDesk/Charge/{id} - Thêm dịch vụ vào bảng kê
    [HttpPost]
    public async Task<IActionResult> Charge(int id, ChargeInput input)
    {
        await Run(() => _frontDesk.Charge(id, input), "Đã ghi dịch vụ vào bảng kê.");
        return RedirectToAction(nameof(Folio), new { id });
    }

    // POST: /Staff/FrontDesk/UpdateCharge/{id} - Sửa số lượng dịch vụ
    [HttpPost]
    public async Task<IActionResult> UpdateCharge(int id, int itemId, int quantity)
    {
        await Run(() => _frontDesk.UpdateCharge(id, itemId, quantity), "Đã cập nhật số lượng.");
        return RedirectToAction(nameof(Folio), new { id });
    }

    // POST: /Staff/FrontDesk/DeleteCharge/{id} - Xóa dịch vụ khỏi bảng kê
    [HttpPost]
    public async Task<IActionResult> DeleteCharge(int id, int itemId)
    {
        await Run(() => _frontDesk.UpdateCharge(id, itemId, 0, delete: true), "Đã xóa khoản dịch vụ.");
        return RedirectToAction(nameof(Folio), new { id });
    }

    // POST: /Staff/FrontDesk/Pay/{id} - Ghi nhận thanh toán tại quầy
    [HttpPost]
    public async Task<IActionResult> Pay(int id, PaymentInput input)
    {
        await Run(() => _frontDesk.Pay(id, input, Actor), "Đã ghi nhận thanh toán đủ. Các khoản phí đã được chốt.");
        return RedirectToAction(nameof(Folio), new { id });
    }

    // POST: /Staff/FrontDesk/CheckOut/{id} - Làm thủ tục trả phòng
    [HttpPost]
    public async Task<IActionResult> CheckOut(int id)
    {
        await Run(() => _frontDesk.CheckOut(id), "Đã hoàn tất trả phòng. Phòng đã chuyển sang trạng thái Cần dọn.");
        return RedirectToAction(nameof(Folio), new { id });
    }
}
