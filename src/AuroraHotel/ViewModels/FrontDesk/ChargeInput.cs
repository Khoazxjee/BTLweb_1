using System.ComponentModel.DataAnnotations;
using AuroraHotel.Models;

namespace AuroraHotel.ViewModels;

public class ChargeInput
{
    [Range(1, int.MaxValue, ErrorMessage = "{0} nằm ngoài phạm vi cho phép.")]
    public int ServiceId { get; set; }

    [Range(1, 1000, ErrorMessage = "{0} nằm ngoài phạm vi cho phép.")]
    public int Quantity { get; set; } = 1;
}
