using AuroraHotel.Models;
using AuroraHotel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuroraHotel.Areas.Staff.Controllers;

[Authorize(Roles = Roles.Admin)]
public class DashboardController(ReportService reports) : StaffController
{
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to)
    {
        try
        {
            if (!ModelState.IsValid)
                throw new BusinessException("Vui lòng nhập ngày báo cáo hợp lệ.");
            var(start, end) = reports.ResolvePeriod(from, to);
            return View(await reports.DashboardAsync(start, end));
        }
        catch (BusinessException exception)
        {
            TempData["Error"] = exception.Message;
            var(start, end) = reports.ResolvePeriod(null, null);
            return View(await reports.DashboardAsync(start, end));
        }
    }

    public async Task<IActionResult> Export(DateOnly? from, DateOnly? to)
    {
        try
        {
            if (!ModelState.IsValid)
                throw new BusinessException("Vui lòng nhập ngày báo cáo hợp lệ.");
            var(start, end) = reports.ResolvePeriod(from, to);
            return File(await reports.ExportPaymentsAsync(start, end), "text/csv; charset=utf-8", $"thu-tien-{start:yyyyMMdd}-{end:yyyyMMdd}.csv");
        }
        catch (BusinessException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
