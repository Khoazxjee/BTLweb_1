using System.ComponentModel.DataAnnotations;
using AuroraHotel.Models;

namespace AuroraHotel.ViewModels;

public record BookingPage(StayInput Input, Room Room, GuestProfile Guest);
