using AuroraHotel.Models;

namespace AuroraHotel.ViewModels;

public sealed record RoomDetailsPage(Room Room, StayInput Stay, string? ReturnUrl)
{
    public int Nights => Math.Max(0, Stay.DepartureDate.DayNumber - Stay.ArrivalDate.DayNumber);
    public decimal EstimatedTotal => Nights * Room.NightlyRate;
}
