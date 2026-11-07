namespace AuroraHotel.Services;

public class HotelClock(Microsoft.Extensions.Options.IOptions<HotelOptions> options) : IHotelClock
{
    public DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone));
    public DateOnly Today => DateOnly.FromDateTime(Now.DateTime);
}
