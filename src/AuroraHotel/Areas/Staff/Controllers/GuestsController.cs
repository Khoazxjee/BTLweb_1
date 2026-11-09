using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Areas.Staff.Controllers;

/// <summary>
/// Controller quản lý danh sách và hồ sơ khách hàng dành cho nhân viên Lễ tân.
/// </summary>
public class GuestsController : StaffController
{
    private readonly HotelDbContext _db;
    private readonly HotelTransaction _transaction;

    public GuestsController(HotelDbContext db, HotelTransaction transaction)
    {
        _db = db;
        _transaction = transaction;
    }

    // GET: /Staff/Guests - Danh sách hồ sơ khách hàng
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        var query = _db.GuestProfiles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.FullName.Contains(q) || x.Email.Contains(q) || x.Phone.Contains(q));

        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (total + 11) / 12));

        var items = await query.OrderBy(x => x.FullName).Skip((page - 1) * 12).Take(12).ToListAsync();
        return View(new PageResult<GuestProfile>(items, total, page, 12));
    }

    // GET: /Staff/Guests/Details/{id} - Xem chi tiết hồ sơ và lịch sử đặt phòng của khách
    public async Task<IActionResult> Details(int id)
    {
        var guest = await _db.GuestProfiles.AsNoTracking()
            .Include(x => x.Reservations).ThenInclude(x => x.Room)
            .SingleOrDefaultAsync(x => x.Id == id);

        return guest == null ? NotFound() : View(guest);
    }

    // GET: /Staff/Guests/Edit - Form thêm mới hoặc cập nhật hồ sơ khách
    public async Task<IActionResult> Edit(int id = 0)
    {
        if (id == 0)
            return View(new GuestInput());

        var guest = await _db.GuestProfiles.FindAsync(id);
        if (guest == null)
            return NotFound();

        return View(new GuestInput
        {
            Id = id,
            FullName = guest.FullName,
            Phone = guest.Phone,
            Email = guest.Email,
            Notes = guest.Notes
        });
    }

    // POST: /Staff/Guests/Edit - Xử lý lưu hồ sơ khách
    [HttpPost]
    public async Task<IActionResult> Edit(GuestInput input)
    {
        if (!ModelState.IsValid)
            return View(input);

        try
        {
            var id = await _transaction.RunAsync(async () =>
            {
                var guest = input.Id == 0
                    ? new GuestProfile()
                    : await _db.GuestProfiles.FindAsync(input.Id) ?? throw new BusinessException("Không tìm thấy khách.");

                guest.FullName = input.FullName.Trim();
                guest.Email = input.Email.Trim();
                guest.Phone = input.Phone.Trim();
                guest.Notes = input.Notes ?? "";

                if (input.Id == 0)
                    _db.GuestProfiles.Add(guest);

                await _db.SaveChangesAsync();
                return guest.Id;
            });

            TempData["Success"] = "Đã lưu hồ sơ khách hàng thành công!";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessException e)
        {
            ModelState.AddModelError("", e.Message);
            return View(input);
        }
    }
}
