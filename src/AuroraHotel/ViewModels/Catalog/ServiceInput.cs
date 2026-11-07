using System.ComponentModel.DataAnnotations;
using AuroraHotel.Models;

namespace AuroraHotel.ViewModels;

public class ServiceInput
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập {0}."), StringLength(100, ErrorMessage = "{0} có độ dài không hợp lệ."), Display(Name = "Tên dịch vụ")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập {0}."), StringLength(30, ErrorMessage = "{0} có độ dài không hợp lệ."), Display(Name = "Đơn vị tính")]
    public string Unit { get; set; } = "lần";

    [Range(0, 1000000000, ErrorMessage = "{0} nằm ngoài phạm vi cho phép."), Display(Name = "Đơn giá (VND)")]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Đang cung cấp")]
    public bool IsActive { get; set; } = true;
}
