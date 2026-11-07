using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Areas.Staff.Controllers;

[Authorize(Roles = Roles.Admin)]
public class CatalogController(HotelDbContext db, CatalogService catalog) : StaffController
{
    public async Task<IActionResult> Rooms(string? q, int? type, int page = 1)
    {
        var query = db.Rooms.AsNoTracking().Include(x => x.RoomType).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.RoomNumber.Contains(q) || x.RoomType.Name.Contains(q));
        if (type.HasValue)
            query = query.Where(x => x.RoomTypeId == type);
        ViewBag.Types = await db.RoomTypes.ToListAsync();
        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (total + 9) / 10));
        return View(new PageResult<Room>(await query.OrderBy(x => x.RoomNumber).Skip((page - 1) * 10).Take(10).ToListAsync(), total, page, 10));
    }

    public async Task<IActionResult> Room(int id = 0)
    {
        ViewBag.Types = await db.RoomTypes.AsNoTracking().ToListAsync();
        if (id == 0)
            return View(new RoomInput());
        var room = await db.Rooms.FindAsync(id);
        if (room == null)
            return NotFound();
        return View(new RoomInput { Id = id, RoomNumber = room.RoomNumber, RoomTypeId = room.RoomTypeId, NightlyRate = room.NightlyRate, Amenities = room.Amenities, Description = room.Description, CurrentImage = room.ImagePath });
    }

    [HttpPost, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Room(RoomInput input)
    {
        if (ModelState.IsValid)
            try
            {
                await catalog.SaveRoom(input);
                TempData["Success"] = "Đã lưu phòng. Thông tin hiện có ở khu vực khách hàng.";
                return RedirectToAction(nameof(Rooms));
            }
            catch (BusinessException e)
            {
                ModelState.AddModelError("", e.Message);
            }

        ViewBag.Types = await db.RoomTypes.ToListAsync();
        input.CurrentImage = input.Id == 0 ? null : await db.Rooms.Where(x => x.Id == input.Id).Select(x => x.ImagePath).FirstOrDefaultAsync();
        return View(input);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        await Run(() => catalog.DeleteRoom(id), "Đã xóa phòng chưa có lịch sử.");
        return RedirectToAction(nameof(Rooms));
    }

    public async Task<IActionResult> Types() => View(await db.RoomTypes.AsNoTracking().OrderBy(x => x.Name).ToListAsync());
    public async Task<IActionResult> Type(int id = 0)
    {
        if (id == 0)
            return View(new RoomTypeInput());
        var type = await db.RoomTypes.FindAsync(id);
        if (type == null)
            return NotFound();
        return View(new RoomTypeInput { Id = id, Name = type.Name, Capacity = type.Capacity, Description = type.Description });
    }

    [HttpPost]
    public async Task<IActionResult> Type(RoomTypeInput input)
    {
        if (ModelState.IsValid)
            try
            {
                await catalog.SaveType(input);
                TempData["Success"] = "Đã lưu loại phòng.";
                return RedirectToAction(nameof(Types));
            }
            catch (BusinessException e)
            {
                ModelState.AddModelError("", e.Message);
            }

        return View(input);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteType(int id)
    {
        await Run(() => catalog.DeleteType(id), "Đã xóa loại phòng.");
        return RedirectToAction(nameof(Types));
    }

    public async Task<IActionResult> Services(string? q, int page = 1) => View(await catalog.Services(q, page));
    public async Task<IActionResult> Service(int id = 0)
    {
        if (id == 0)
            return View(new ServiceInput());
        var service = await db.Services.FindAsync(id);
        if (service == null)
            return NotFound();
        return View(new ServiceInput { Id = id, Name = service.Name, Unit = service.Unit, UnitPrice = service.UnitPrice, IsActive = service.IsActive });
    }

    [HttpPost]
    public async Task<IActionResult> Service(ServiceInput input)
    {
        if (ModelState.IsValid)
            try
            {
                await catalog.SaveService(input);
                TempData["Success"] = "Đã lưu dịch vụ.";
                return RedirectToAction(nameof(Services));
            }
            catch (BusinessException e)
            {
                ModelState.AddModelError("", e.Message);
            }

        return View(input);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteService(int id)
    {
        await Run(() => catalog.DeleteService(id), "Đã xóa dịch vụ chưa sử dụng hoặc ngừng cung cấp để giữ lịch sử.");
        return RedirectToAction(nameof(Services));
    }
}
