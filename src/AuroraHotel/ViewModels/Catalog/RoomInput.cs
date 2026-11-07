using System.ComponentModel.DataAnnotations;
using AuroraHotel.Models;

namespace AuroraHotel.ViewModels;

public class RoomInput
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập {0}."), StringLength(20, ErrorMessage = "{0} có độ dài không hợp lệ."), Display(Name = "Số phòng")]
    public string RoomNumber { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "{0} nằm ngoài phạm vi cho phép."), Display(Name = "Loại phòng")]
    public int RoomTypeId { get; set; }

    [Range(1, 1000000000, ErrorMessage = "{0} nằm ngoài phạm vi cho phép."), Display(Name = "Giá mỗi đêm (VND)")]
    public decimal NightlyRate { get; set; } = 800000;

    [StringLength(12000, ErrorMessage = "{0} có độ dài không hợp lệ."), Display(Name = "Mô tả phòng")]
    public string? Description { get; set; }

    [StringLength(1000, ErrorMessage = "{0} có độ dài không hợp lệ."), Display(Name = "Tiện nghi (ngăn cách bằng dấu phẩy)")]
    public string? Amenities { get; set; }

    [Display(Name = "Ảnh phòng (JPEG, PNG, WebP; tối đa 5 MB)")]
    public IFormFile? Image { get; set; }
    public string? CurrentImage { get; set; }
}
