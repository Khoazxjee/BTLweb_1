using System.ComponentModel.DataAnnotations;

namespace AuroraHotel.Models;

public class Folio
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;
    public FolioStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public List<FolioItem> Items { get; set; } = [];
    public Payment? Payment { get; set; }
    public decimal Total => Items.Sum(x => x.Quantity * x.UnitPriceSnapshot);
    public decimal Paid => Payment?.Amount ?? 0;
    public decimal Balance => Total - Paid;
}
