using System.ComponentModel.DataAnnotations;

namespace AuroraHotel.Models;

public class Reservation
{
    public int Id { get; set; }

    [MaxLength(30)]
    public string ConfirmationCode { get; set; } = "";
    public int GuestProfileId { get; set; }
    public GuestProfile GuestProfile { get; set; } = null!;
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public DateOnly ArrivalDate { get; set; }
    public DateOnly DepartureDate { get; set; }
    public int GuestCount { get; set; }
    public decimal NightlyRateSnapshot { get; set; }
    public ReservationStatus Status { get; set; }
    public ReservationSource Source { get; set; }
    public string CreatedByUserId { get; set; } = "";
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CheckInAt { get; set; }
    public DateTimeOffset? CheckOutAt { get; set; }

    [MaxLength(2000)]
    public string Notes { get; set; } = "";

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
    public Folio? Folio { get; set; }
    public int Nights => DepartureDate.DayNumber - ArrivalDate.DayNumber;
    public decimal RoomTotal => Nights * NightlyRateSnapshot;
}
