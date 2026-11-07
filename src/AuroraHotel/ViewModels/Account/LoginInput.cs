using System.ComponentModel.DataAnnotations;
using AuroraHotel.Models;

namespace AuroraHotel.ViewModels;

public class LoginInput
{
    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email chưa đúng định dạng."), Display(Name = "Email đăng nhập")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu."), DataType(DataType.Password), Display(Name = "Mật khẩu")]
    public string Password { get; set; } = "";

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}
