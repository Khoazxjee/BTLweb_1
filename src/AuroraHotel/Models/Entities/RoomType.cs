using System.ComponentModel.DataAnnotations;

namespace AuroraHotel.Models;

public class RoomType
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = "";
    public int Capacity { get; set; }

    [MaxLength(2000)]
    public string Description { get; set; } = "";
    public List<Room> Rooms { get; set; } = [];
}
