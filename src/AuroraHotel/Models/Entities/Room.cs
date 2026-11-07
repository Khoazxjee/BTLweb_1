using System.ComponentModel.DataAnnotations;

namespace AuroraHotel.Models;

public class Room
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string RoomNumber { get; set; } = "";
    public int RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;
    public decimal NightlyRate { get; set; }

    [MaxLength(500)]
    public string ImagePath { get; set; } = "/images/room-deluxe.jpg";

    [MaxLength(12000)]
    public string Description { get; set; } = "";

    [MaxLength(1000)]
    public string Amenities { get; set; } = "";
    public HousekeepingStatus HousekeepingStatus { get; set; }
    public OperationalStatus OperationalStatus { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
    public List<Reservation> Reservations { get; set; } = [];
}
