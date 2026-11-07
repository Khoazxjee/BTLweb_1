using System.ComponentModel.DataAnnotations;

namespace AuroraHotel.Models;

public class HotelService
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = "";

    [MaxLength(30)]
    public string Unit { get; set; } = "lần";
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
}
