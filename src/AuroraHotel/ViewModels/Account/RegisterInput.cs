using System.ComponentModel.DataAnnotations;
using AuroraHotel.Models;

namespace AuroraHotel.ViewModels;

public class RegisterInput
{
    [Required(ErrorMessage = "Vui lòng nhập {0}."), StringLength(100, MinimumLength = 2, ErrorMessage = "{0} có độ dài không hợp lệ."), Display(Name = "Họ và tên")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập {0}."), EmailAddress(ErrorMessage = "Email chưa đúng định dạng."), StringLength(200, ErrorMessage = "{0} có độ dài không hợp lệ."), Display(Name = "Email đăng nhập")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập {0}."), Phone(ErrorMessage = "Số điện thoại chưa đúng định dạng."), StringLength(30, ErrorMessage = "{0} có độ dài không hợp lệ."), Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập {0}."), StringLength(100, MinimumLength = 8, ErrorMessage = "{0} có độ dài không hợp lệ."), DataType(DataType.Password), Display(Name = "Mật khẩu")]
    public string Password { get; set; } = "";

    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận chưa khớp."), DataType(DataType.Password), Display(Name = "Nhập lại mật khẩu")]
    public string ConfirmPassword { get; set; } = "";
    public string? ReturnUrl { get; set; }
}
