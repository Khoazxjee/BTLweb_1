using System.ComponentModel.DataAnnotations;

namespace AuroraHotel.Models;

public class GuestProfile
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [MaxLength(100)]
    public string FullName { get; set; } = "";

    [MaxLength(30)]
    public string Phone { get; set; } = "";

    [MaxLength(200)]
    public string Email { get; set; } = "";

    [MaxLength(2000)]
    public string Notes { get; set; } = "";
    public List<Reservation> Reservations { get; set; } = [];
}
