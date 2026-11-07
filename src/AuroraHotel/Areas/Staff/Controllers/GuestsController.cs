using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Areas.Staff.Controllers;

public class GuestsController(HotelDbContext db, HotelTransaction transaction) : StaffController
{
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        var query = db.GuestProfiles.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.FullName.Contains(q) || x.Email.Contains(q) || x.Phone.Contains(q));
        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (total + 11) / 12));
        return View(new PageResult<GuestProfile>(await query.OrderBy(x => x.FullName).Skip((page - 1) * 12).Take(12).ToListAsync(), total, page, 12));
    }

    public async Task<IActionResult> Details(int id)
    {
        var guest = await db.GuestProfiles.AsNoTracking().Include(x => x.Reservations).ThenInclude(x => x.Room).SingleOrDefaultAsync(x => x.Id == id);
        return guest == null ? NotFound() : View(guest);
    }

    public async Task<IActionResult> Edit(int id = 0)
    {
        if (id == 0)
            return View(new GuestInput());
        var guest = await db.GuestProfiles.FindAsync(id);
        if (guest == null)
            return NotFound();
        return View(new GuestInput { Id = id, FullName = guest.FullName, Phone = guest.Phone, Email = guest.Email, Notes = guest.Notes });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(GuestInput input)
    {
        if (!ModelState.IsValid)
            return View(input);
        try
        {
            var id = await transaction.RunAsync(async () =>
            {
                var guest = input.Id == 0 ? new GuestProfile() : await db.GuestProfiles.FindAsync(input.Id) ?? throw new BusinessException("Không tìm thấy khách.");
                guest.FullName = input.FullName.Trim();
                guest.Email = input.Email.Trim();
                guest.Phone = input.Phone.Trim();
                guest.Notes = input.Notes ?? "";
                if (input.Id == 0)
                    db.GuestProfiles.Add(guest);
                await db.SaveChangesAsync();
                return guest.Id;
            });
            TempData["Success"] = "Đã lưu hồ sơ khách.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessException e)
        {
            ModelState.AddModelError("", e.Message);
            return View(input);
        }
    }
}
