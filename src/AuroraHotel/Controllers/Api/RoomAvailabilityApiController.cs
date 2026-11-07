using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Controllers;

[ApiController]
public class RoomAvailabilityApiController(RoomAvailabilityService availability) : ControllerBase
{
    [HttpGet("/api/rooms/availability")]
    public async Task<IActionResult> Availability([FromQuery] SearchInput input)
    {
        try
        {
            var page = await availability.Search(input);
            return Ok(new { page.Total, page.Page, page.PageSize, page.Pages, Items = page.Items.Select(x => new { x.Id, x.RoomNumber, RoomType = x.RoomType.Name, x.RoomType.Capacity, x.NightlyRate, x.ImagePath, x.Amenities, Nights = input.DepartureDate!.Value.DayNumber - input.ArrivalDate!.Value.DayNumber, EstimatedTotal = (input.DepartureDate.Value.DayNumber - input.ArrivalDate.Value.DayNumber) * x.NightlyRate }) });
        }
        catch (BusinessException e)
        {
            return BadRequest(new { error = e.Message });
        }
    }
}
