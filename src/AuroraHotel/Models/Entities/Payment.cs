namespace AuroraHotel.Models;

public class Payment
{
    public int Id { get; set; }
    public int FolioId { get; set; }
    public Folio Folio { get; set; } = null!;
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTimeOffset PaidAt { get; set; }
    public string ReceivedByUserId { get; set; } = "";
    public ApplicationUser ReceivedByUser { get; set; } = null!;
}
