using AuroraHotel.Data;
using AuroraHotel.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Controllers;

public class HomeController(HotelDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View((await db.Rooms.AsNoTracking().Include(x => x.RoomType).Where(x => x.OperationalStatus == OperationalStatus.Active).OrderBy(x => x.RoomNumber).ToListAsync()).GroupBy(x => x.RoomTypeId).Select(g => g.First()).ToList());
    public IActionResult Error()
    {
        Response.StatusCode = 500;
        ViewBag.Code = 500;
        return View("Status");
    }

    public IActionResult Status(int code)
    {
        Response.StatusCode = code;
        ViewBag.Code = code;
        return View();
    }
}
