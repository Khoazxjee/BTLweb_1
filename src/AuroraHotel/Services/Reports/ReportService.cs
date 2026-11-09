using System.Globalization;
using System.Text;
using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Services;

/// <summary>
/// Service thống kê báo cáo và xuất dữ liệu cho quản trị viên.
/// </summary>
public sealed class ReportService
{
    private readonly HotelDbContext _db;
    private readonly IHotelClock _clock;

    public ReportService(HotelDbContext db, IHotelClock clock)
    {
        _db = db;
        _clock = clock;
    }

    // Xác định khoảng thời gian báo cáo (mặc định từ đầu tháng đến hôm nay)
    public (DateOnly Start, DateOnly End) ResolvePeriod(DateOnly? from, DateOnly? to)
    {
        var start = from ?? new DateOnly(_clock.Today.Year, _clock.Today.Month, 1);
        var end = to ?? _clock.Today;
        if (end < start || end == DateOnly.MaxValue)
            throw new BusinessException("Khoảng ngày báo cáo không hợp lệ.");
        return (start, end);
    }

    // Lọc danh sách thanh toán trong khoảng thời gian chỉ định
    private IQueryable<Payment> PaymentsInPeriod(DateOnly start, DateOnly end)
    {
        var lower = new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue), _clock.Now.Offset);
        var upper = new DateTimeOffset(end.AddDays(1).ToDateTime(TimeOnly.MinValue), _clock.Now.Offset);
        return _db.Payments.AsNoTracking().Where(x => x.PaidAt >= lower && x.PaidAt < upper);
    }

    // Thống kê tổng quan cho trang Dashboard quản trị
    public async Task<DashboardModel> DashboardAsync(DateOnly start, DateOnly end)
    {
        var lower = new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue), _clock.Now.Offset);
        var upper = new DateTimeOffset(end.AddDays(1).ToDateTime(TimeOnly.MinValue), _clock.Now.Offset);
        var payments = PaymentsInPeriod(start, end);

        // Đếm số lượng đơn theo từng trạng thái
        var counts = await _db.Reservations
            .Where(x => x.CreatedAt >= lower && x.CreatedAt < upper)
            .GroupBy(x => x.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        // Đếm số phòng sẵn sàng đón khách (phải sạch, đang hoạt động và không có ai đặt hôm nay)
        var ready = await _db.Rooms.CountAsync(room =>
            room.OperationalStatus == OperationalStatus.Active &&
            room.HousekeepingStatus == HousekeepingStatus.Clean &&
            !room.Reservations.Any(stay =>
                stay.Status == ReservationStatus.CheckedIn ||
                (stay.Status == ReservationStatus.Confirmed &&
                 stay.ArrivalDate <= _clock.Today &&
                 stay.DepartureDate > _clock.Today)));

        var occupied = await _db.Reservations.CountAsync(x => x.Status == ReservationStatus.CheckedIn);
        var dirty = await _db.Rooms.CountAsync(x => x.HousekeepingStatus == HousekeepingStatus.Dirty);
        var revenue = await payments.SumAsync(x => (decimal?)x.Amount) ?? 0;
        var paymentCount = await payments.CountAsync();
        var recent = await _db.Reservations.AsNoTracking()
            .Include(x => x.Room)
            .Include(x => x.GuestProfile)
            .OrderByDescending(x => x.CreatedAt)
            .Take(6)
            .ToListAsync();

        return new DashboardModel(start, end, occupied, dirty, ready, revenue, paymentCount, counts, recent);
    }

    // Xuất danh sách giao dịch thanh toán ra định dạng file CSV
    public async Task<byte[]> ExportPaymentsAsync(DateOnly start, DateOnly end)
    {
        // 1. Lấy dữ liệu thanh toán từ CSDL
        var payments = await PaymentsInPeriod(start, end)
            .OrderBy(x => x.PaidAt)
            .Select(x => new
            {
                x.Folio.Reservation.ConfirmationCode,
                x.Folio.Reservation.GuestProfile.FullName,
                x.Folio.Reservation.Room.RoomNumber,
                x.PaidAt,
                x.Method,
                x.Amount
            })
            .ToListAsync();

        // 2. Tạo nội dung file CSV
        var csv = new StringBuilder("Mã đặt phòng,Họ tên,Phòng,Ngày thu,Phương thức,Số tiền VND\r\n");
        foreach (var payment in payments)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                CsvCell(payment.ConfirmationCode),
                CsvCell(payment.FullName),
                CsvCell(payment.RoomNumber),
                CsvCell(Ui.Time(payment.PaidAt)),
                CsvCell(payment.Method.Label()),
                payment.Amount.ToString("0", CultureInfo.InvariantCulture)
            }));
        }

        // Thêm UTF-8 BOM để Excel hiển thị tiếng Việt không bị lỗi font
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    // Hàm bao bọc ô dữ liệu trong dấu nháy kép chuẩn CSV
    private static string CsvCell(string value)
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
