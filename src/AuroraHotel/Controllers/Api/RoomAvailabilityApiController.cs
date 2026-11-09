using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AuroraHotel.Controllers;

/// <summary>
/// RESTful API tra cứu danh sách phòng khả dụng theo tiêu chí tìm kiếm.
/// </summary>
[ApiController]
public class RoomAvailabilityApiController : ControllerBase
{
    private readonly RoomAvailabilityService _availability;

    public RoomAvailabilityApiController(RoomAvailabilityService availability)
    {
        _availability = availability;
    }

    // GET: /api/rooms/availability
    [HttpGet("/api/rooms/availability")]
    public async Task<IActionResult> Availability([FromQuery] SearchInput input)
    {
        try
        {
            var page = await _availability.Search(input);
            return Ok(new
            {
                page.Total,
                page.Page,
                page.PageSize,
                page.Pages,
                Items = page.Items.Select(x => new
                {
                    x.Id,
                    x.RoomNumber,
                    RoomType = x.RoomType.Name,
                    x.RoomType.Capacity,
                    x.NightlyRate,
                    x.ImagePath,
                    x.Amenities,
                    Nights = input.DepartureDate!.Value.DayNumber - input.ArrivalDate!.Value.DayNumber,
                    EstimatedTotal = (input.DepartureDate.Value.DayNumber - input.ArrivalDate.Value.DayNumber) * x.NightlyRate
                })
            });
        }
        catch (BusinessException e)
        {
            return BadRequest(new { error = e.Message });
        }
    }
}
