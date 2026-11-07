using System.ComponentModel.DataAnnotations;
using AuroraHotel.Models;

namespace AuroraHotel.ViewModels;

public class RoomTypeInput
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập {0}."), StringLength(100, ErrorMessage = "{0} có độ dài không hợp lệ."), Display(Name = "Tên loại phòng")]
    public string Name { get; set; } = "";

    [Range(1, 20, ErrorMessage = "{0} nằm ngoài phạm vi cho phép."), Display(Name = "Sức chứa")]
    public int Capacity { get; set; } = 2;

    [StringLength(2000, ErrorMessage = "{0} có độ dài không hợp lệ."), Display(Name = "Mô tả")]
    public string? Description { get; set; }
}
