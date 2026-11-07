namespace AuroraHotel.Services;

public interface IHotelClock
{
    DateTimeOffset Now { get; }

    DateOnly Today { get; }
}
