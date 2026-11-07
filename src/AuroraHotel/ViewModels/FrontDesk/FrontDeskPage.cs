using AuroraHotel.Models;

namespace AuroraHotel.ViewModels;

public sealed record FrontDeskPage(string Tab, DateOnly Today, int Arrivals, int OverdueArrivals, int InHouse, int Departures, IReadOnlyList<Reservation> Reservations);
