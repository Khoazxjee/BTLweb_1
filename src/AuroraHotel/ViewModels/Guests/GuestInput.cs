using System.ComponentModel.DataAnnotations;
using AuroraHotel.Models;

namespace AuroraHotel.ViewModels;

public class GuestInput
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập {0}."), StringLength(100, MinimumLength = 2, ErrorMessage = "{0} có độ dài không hợp lệ."), Display(Name = "Họ và tên")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập {0}."), Phone(ErrorMessage = "Số điện thoại chưa đúng định dạng."), StringLength(30, ErrorMessage = "{0} có độ dài không hợp lệ."), Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập {0}."), EmailAddress(ErrorMessage = "Email chưa đúng định dạng."), StringLength(200, ErrorMessage = "{0} có độ dài không hợp lệ."), Display(Name = "Email liên hệ")]
    public string Email { get; set; } = "";

    [StringLength(2000, ErrorMessage = "{0} có độ dài không hợp lệ."), Display(Name = "Ghi chú")]
    public string? Notes { get; set; }
}
