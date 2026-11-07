using System.ComponentModel.DataAnnotations;

namespace AuroraHotel.Models;

public class FolioItem
{
    public int Id { get; set; }
    public int FolioId { get; set; }
    public Folio Folio { get; set; } = null!;
    public ItemKind Kind { get; set; }
    public int? ServiceId { get; set; }
    public HotelService? Service { get; set; }

    [MaxLength(300)]
    public string DescriptionSnapshot { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public decimal Total => Quantity * UnitPriceSnapshot;
}
