using System.ComponentModel.DataAnnotations;
using AuroraHotel.Models;

namespace AuroraHotel.ViewModels;

public class PaymentInput
{
    [Range(1, 1000000000000, ErrorMessage = "{0} nằm ngoài phạm vi cho phép.")]
    public decimal Amount { get; set; }

    [EnumDataType(typeof(PaymentMethod))]
    public PaymentMethod Method { get; set; }
}
