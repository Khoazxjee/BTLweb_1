using System.Security.Claims;
using AuroraHotel.Models;
using AuroraHotel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuroraHotel.Areas.Staff.Controllers;

[Area("Staff"), Authorize(Roles = Roles.Staff)]
public abstract class StaffController : Controller
{
    protected string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    protected async Task Run(Func<Task> action, string success)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu chưa hợp lệ. Vui lòng kiểm tra lại.";
            return;
        }

        try
        {
            await action();
            TempData["Success"] = success;
        }
        catch (BusinessException e)
        {
            TempData["Error"] = e.Message;
        }
    }
}
