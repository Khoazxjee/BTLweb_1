using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Controllers;

/// <summary>
/// RESTful API quản lý danh mục dịch vụ (GET/POST/PUT/DELETE) - Yêu cầu xác thực quyền Nhân viên / Admin.
/// </summary>
[ApiController]
[Route("api/services")]
[Authorize(Roles = Roles.Staff)]
public class ServicesApiController : ControllerBase
{
    private readonly HotelDbContext _db;
    private readonly CatalogService _catalog;

    public ServicesApiController(HotelDbContext db, CatalogService catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    // GET: /api/services - Danh sách dịch vụ
    [HttpGet]
    public async Task<IActionResult> Index(string? q, int page = 1) =>
        Ok(await _catalog.Services(q, page));

    // GET: /api/services/{id} - Chi tiết dịch vụ
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var item = await _db.Services.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        return item == null ? NotFound() : Ok(item);
    }

    // POST: /api/services - Thêm mới dịch vụ (chỉ Admin)
    [HttpPost, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(ServiceInput input)
    {
        input.Id = 0;
        try
        {
            var id = await _catalog.SaveService(input);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }
        catch (BusinessException e)
        {
            return BadRequest(new { error = e.Message });
        }
    }

    // PUT: /api/services/{id} - Cập nhật dịch vụ (chỉ Admin)
    [HttpPut("{id:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(int id, ServiceInput input)
    {
        if (!await _db.Services.AnyAsync(x => x.Id == id))
            return NotFound();

        input.Id = id;
        try
        {
            await _catalog.SaveService(input);
            return NoContent();
        }
        catch (BusinessException e)
        {
            return Conflict(new { error = e.Message });
        }
    }

    // DELETE: /api/services/{id} - Xóa dịch vụ (chỉ Admin)
    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _db.Services.AnyAsync(x => x.Id == id))
            return NotFound();

        try
        {
            await _catalog.DeleteService(id);
            return NoContent();
        }
        catch (BusinessException e)
        {
            return Conflict(new { error = e.Message });
        }
    }
}
