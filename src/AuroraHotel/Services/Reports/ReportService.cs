using System.Globalization;
using System.Text;
using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Services;

public sealed class ReportService(HotelDbContext db, IHotelClock clock)
{
    public (DateOnly Start, DateOnly End) ResolvePeriod(DateOnly? from, DateOnly? to)
    {
        var start = from ?? new DateOnly(clock.Today.Year, clock.Today.Month, 1);
        var end = to ?? clock.Today;
        if (end < start || end == DateOnly.MaxValue)
            throw new BusinessException("Khoảng ngày báo cáo không hợp lệ.");
        return (start, end);
    }

    private IQueryable<Payment> PaymentsInPeriod(DateOnly start, DateOnly end)
    {
        var lower = new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue), clock.Now.Offset);
        var upper = new DateTimeOffset(end.AddDays(1).ToDateTime(TimeOnly.MinValue), clock.Now.Offset);
        return db.Payments.AsNoTracking().Where(x => x.PaidAt >= lower && x.PaidAt < upper);
    }

    public async Task<DashboardModel> DashboardAsync(DateOnly start, DateOnly end)
    {
        var lower = new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue), clock.Now.Offset);
        var upper = new DateTimeOffset(end.AddDays(1).ToDateTime(TimeOnly.MinValue), clock.Now.Offset);
        var payments = PaymentsInPeriod(start, end);
        var counts = await db.Reservations
            .Where(x => x.CreatedAt >= lower && x.CreatedAt < upper)
            .GroupBy(x => x.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        // Phòng sẵn sàng phải sạch, hoạt động và chưa được giữ cho hôm nay.
        var ready = await db.Rooms.CountAsync(room =>
            room.OperationalStatus == OperationalStatus.Active &&
            room.HousekeepingStatus == HousekeepingStatus.Clean &&
            !room.Reservations.Any(stay =>
                stay.Status == ReservationStatus.CheckedIn ||
                (stay.Status == ReservationStatus.Confirmed &&
                 stay.ArrivalDate <= clock.Today &&
                 stay.DepartureDate > clock.Today)));

        var occupied = await db.Reservations.CountAsync(x => x.Status == ReservationStatus.CheckedIn);
        var dirty = await db.Rooms.CountAsync(x => x.HousekeepingStatus == HousekeepingStatus.Dirty);
        var revenue = await payments.SumAsync(x => (decimal?)x.Amount) ?? 0;
        var paymentCount = await payments.CountAsync();
        var recent = await db.Reservations.AsNoTracking()
            .Include(x => x.Room)
            .Include(x => x.GuestProfile)
            .OrderByDescending(x => x.CreatedAt)
            .Take(6)
            .ToListAsync();

        return new DashboardModel(start, end, occupied, dirty, ready, revenue, paymentCount, counts, recent);
    }

    public async Task<byte[]> ExportPaymentsAsync(DateOnly start, DateOnly end)
    {
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

        // BOM giúp Excel đọc tiếng Việt; văn bản người dùng không được thành công thức.
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    private static string CsvCell(string value)
    {
        var trimmed = value.TrimStart();
        if (trimmed.Length > 0 && "=+-@".Contains(trimmed[0]))
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
