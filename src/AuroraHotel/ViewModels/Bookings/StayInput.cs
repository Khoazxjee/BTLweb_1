using System.ComponentModel.DataAnnotations;
using AuroraHotel.Models;

namespace AuroraHotel.ViewModels;

public class StayInput
{
    [Range(1, int.MaxValue, ErrorMessage = "{0} nằm ngoài phạm vi cho phép."), Display(Name = "Phòng")]
    public int RoomId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập {0}."), DataType(DataType.Date), Display(Name = "Ngày nhận phòng")]
    public DateOnly ArrivalDate { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập {0}."), DataType(DataType.Date), Display(Name = "Ngày trả phòng")]
    public DateOnly DepartureDate { get; set; }

    [Range(1, 20, ErrorMessage = "{0} nằm ngoài phạm vi cho phép."), Display(Name = "Số khách")]
    public int GuestCount { get; set; } = 2;

    [StringLength(2000, ErrorMessage = "{0} có độ dài không hợp lệ."), Display(Name = "Ghi chú cho khách sạn")]
    public string? Notes { get; set; }
}
