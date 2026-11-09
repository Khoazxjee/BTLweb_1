using AuroraHotel.Models;
using AuroraHotel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuroraHotel.Areas.Staff.Controllers;

/// <summary>
/// Controller hiển thị màn hình Dashboard thống kê doanh thu và xuất báo cáo cho Admin.
/// </summary>
[Authorize(Roles = Roles.Admin)]
public class DashboardController : StaffController
{
    private readonly ReportService _reports;

    public DashboardController(ReportService reports)
    {
        _reports = reports;
    }

    // GET: /Staff/Dashboard - Thống kê tổng quan tình hình kinh doanh
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to)
    {
        try
        {
            if (!ModelState.IsValid)
                throw new BusinessException("Vui lòng nhập ngày báo cáo hợp lệ.");

            // Xác định khoảng thời gian báo cáo
            var (start, end) = _reports.ResolvePeriod(from, to);
            return View(await _reports.DashboardAsync(start, end));
        }
        catch (BusinessException exception)
        {
            TempData["Error"] = exception.Message;
            var (start, end) = _reports.ResolvePeriod(null, null);
            return View(await _reports.DashboardAsync(start, end));
        }
    }

    // GET: /Staff/Dashboard/Export - Xuất báo cáo thu tiền ra file CSV
    public async Task<IActionResult> Export(DateOnly? from, DateOnly? to)
    {
        try
        {
            if (!ModelState.IsValid)
                throw new BusinessException("Vui lòng nhập ngày báo cáo hợp lệ.");

            var (start, end) = _reports.ResolvePeriod(from, to);
            var fileBytes = await _reports.ExportPaymentsAsync(start, end);
            return File(fileBytes, "text/csv; charset=utf-8", $"thu-tien-{start:yyyyMMdd}-{end:yyyyMMdd}.csv");
        }
        catch (BusinessException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
