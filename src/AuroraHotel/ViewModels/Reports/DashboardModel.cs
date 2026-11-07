using System.ComponentModel.DataAnnotations;
using AuroraHotel.Models;

namespace AuroraHotel.ViewModels;

public record DashboardModel(DateOnly From, DateOnly To, int Occupied, int Dirty, int Available, decimal Revenue, int PaymentCount, Dictionary<ReservationStatus, int> Counts, List<Reservation> Recent);
