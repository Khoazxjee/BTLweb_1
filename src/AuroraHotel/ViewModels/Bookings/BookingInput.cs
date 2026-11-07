using System.ComponentModel.DataAnnotations;
using AuroraHotel.Models;

namespace AuroraHotel.ViewModels;

public class BookingInput : StayInput
{
    [Range(1, int.MaxValue, ErrorMessage = "{0} nằm ngoài phạm vi cho phép."), Display(Name = "Khách hàng")]
    public int GuestProfileId { get; set; }
}
