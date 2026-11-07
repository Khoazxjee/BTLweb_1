using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Controllers;

[ApiController, Route("api/services"), Authorize(Roles = Roles.Staff)]
public class ServicesApiController(HotelDbContext db, CatalogService catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q, int page = 1) => Ok(await catalog.Services(q, page));
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var item = await db.Services.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(ServiceInput input)
    {
        input.Id = 0;
        try
        {
            var id = await catalog.SaveService(input);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }
        catch (BusinessException e)
        {
            return BadRequest(new { error = e.Message });
        }
    }

    [HttpPut("{id:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(int id, ServiceInput input)
    {
        if (!await db.Services.AnyAsync(x => x.Id == id))
            return NotFound();
        input.Id = id;
        try
        {
            await catalog.SaveService(input);
            return NoContent();
        }
        catch (BusinessException e)
        {
            return Conflict(new { error = e.Message });
        }
    }

    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await db.Services.AnyAsync(x => x.Id == id))
            return NotFound();
        try
        {
            await catalog.DeleteService(id);
            return NoContent();
        }
        catch (BusinessException e)
        {
            return Conflict(new { error = e.Message });
        }
    }
}
