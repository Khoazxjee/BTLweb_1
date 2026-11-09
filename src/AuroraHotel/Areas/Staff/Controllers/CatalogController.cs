using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Areas.Staff.Controllers;

/// <summary>
/// Controller quản lý danh mục (CRUD Phòng, Loại phòng, Dịch vụ đi kèm) - Chỉ dành cho Quản trị viên (Admin).
/// </summary>
[Authorize(Roles = Roles.Admin)]
public class CatalogController : StaffController
{
    private readonly HotelDbContext _db;
    private readonly CatalogService _catalog;

    public CatalogController(HotelDbContext db, CatalogService catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    #region Quản lý Phòng (Rooms)

    // GET: /Staff/Catalog/Rooms - Danh sách phòng có tìm kiếm và phân trang
    public async Task<IActionResult> Rooms(string? q, int? type, int page = 1)
    {
        var query = _db.Rooms.AsNoTracking().Include(x => x.RoomType).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.RoomNumber.Contains(q) || x.RoomType.Name.Contains(q));

        if (type.HasValue)
            query = query.Where(x => x.RoomTypeId == type);

        ViewBag.Types = await _db.RoomTypes.ToListAsync();

        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (total + 9) / 10));

        var items = await query.OrderBy(x => x.RoomNumber).Skip((page - 1) * 10).Take(10).ToListAsync();
        return View(new PageResult<Room>(items, total, page, 10));
    }

    // GET: /Staff/Catalog/Room - Form thêm mới hoặc sửa phòng
    public async Task<IActionResult> Room(int id = 0)
    {
        ViewBag.Types = await _db.RoomTypes.AsNoTracking().ToListAsync();

        if (id == 0)
            return View(new RoomInput());

        var room = await _db.Rooms.FindAsync(id);
        if (room == null)
            return NotFound();

        return View(new RoomInput
        {
            Id = id,
            RoomNumber = room.RoomNumber,
            RoomTypeId = room.RoomTypeId,
            NightlyRate = room.NightlyRate,
            Amenities = room.Amenities,
            Description = room.Description,
            CurrentImage = room.ImagePath
        });
    }

    // POST: /Staff/Catalog/Room - Xử lý lưu thông tin phòng
    [HttpPost, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Room(RoomInput input)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _catalog.SaveRoom(input);
                TempData["Success"] = "Đã lưu thông tin phòng thành công!";
                return RedirectToAction(nameof(Rooms));
            }
            catch (BusinessException e)
            {
                ModelState.AddModelError("", e.Message);
            }
        }

        ViewBag.Types = await _db.RoomTypes.ToListAsync();
        input.CurrentImage = input.Id == 0 ? null : await _db.Rooms.Where(x => x.Id == input.Id).Select(x => x.ImagePath).FirstOrDefaultAsync();
        return View(input);
    }

    // POST: /Staff/Catalog/DeleteRoom/{id} - Xóa phòng
    [HttpPost]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        await Run(() => _catalog.DeleteRoom(id), "Đã xóa phòng thành công.");
        return RedirectToAction(nameof(Rooms));
    }

    #endregion

    #region Quản lý Loại phòng (Room Types)

    // GET: /Staff/Catalog/Types - Danh sách các loại phòng
    public async Task<IActionResult> Types() =>
        View(await _db.RoomTypes.AsNoTracking().OrderBy(x => x.Name).ToListAsync());

    // GET: /Staff/Catalog/Type - Form thêm/sửa loại phòng
    public async Task<IActionResult> Type(int id = 0)
    {
        if (id == 0)
            return View(new RoomTypeInput());

        var type = await _db.RoomTypes.FindAsync(id);
        if (type == null)
            return NotFound();

        return View(new RoomTypeInput
        {
            Id = id,
            Name = type.Name,
            Capacity = type.Capacity,
            Description = type.Description
        });
    }

    // POST: /Staff/Catalog/Type - Xử lý lưu loại phòng
    [HttpPost]
    public async Task<IActionResult> Type(RoomTypeInput input)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _catalog.SaveType(input);
                TempData["Success"] = "Đã lưu loại phòng thành công!";
                return RedirectToAction(nameof(Types));
            }
            catch (BusinessException e)
            {
                ModelState.AddModelError("", e.Message);
            }
        }

        return View(input);
    }

    // POST: /Staff/Catalog/DeleteType/{id} - Xóa loại phòng
    [HttpPost]
    public async Task<IActionResult> DeleteType(int id)
    {
        await Run(() => _catalog.DeleteType(id), "Đã xóa loại phòng thành công.");
        return RedirectToAction(nameof(Types));
    }

    #endregion

    #region Quản lý Dịch vụ (Services)

    // GET: /Staff/Catalog/Services - Danh sách dịch vụ
    public async Task<IActionResult> Services(string? q, int page = 1) =>
        View(await _catalog.Services(q, page));

    // GET: /Staff/Catalog/Service - Form thêm/sửa dịch vụ
    public async Task<IActionResult> Service(int id = 0)
    {
        if (id == 0)
            return View(new ServiceInput());

        var service = await _db.Services.FindAsync(id);
        if (service == null)
            return NotFound();

        return View(new ServiceInput
        {
            Id = id,
            Name = service.Name,
            Unit = service.Unit,
            UnitPrice = service.UnitPrice,
            IsActive = service.IsActive
        });
    }

    // POST: /Staff/Catalog/Service - Xử lý lưu dịch vụ
    [HttpPost]
    public async Task<IActionResult> Service(ServiceInput input)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _catalog.SaveService(input);
                TempData["Success"] = "Đã lưu dịch vụ thành công!";
                return RedirectToAction(nameof(Services));
            }
            catch (BusinessException e)
            {
                ModelState.AddModelError("", e.Message);
            }
        }

        return View(input);
    }

    // POST: /Staff/Catalog/DeleteService/{id} - Xóa hoặc ngừng cung cấp dịch vụ
    [HttpPost]
    public async Task<IActionResult> DeleteService(int id)
    {
        await Run(() => _catalog.DeleteService(id), "Đã xóa dịch vụ hoặc chuyển sang ngừng cung cấp.");
        return RedirectToAction(nameof(Services));
    }

    #endregion
}
