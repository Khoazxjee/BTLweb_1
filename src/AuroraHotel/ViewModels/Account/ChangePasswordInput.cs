using System.ComponentModel.DataAnnotations;

namespace AuroraHotel.ViewModels;

public sealed class ChangePasswordInput
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    [DataType(DataType.Password), Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu cần từ 8 đến 100 ký tự.")]
    [DataType(DataType.Password), Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = "";

    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận chưa khớp.")]
    [DataType(DataType.Password), Display(Name = "Nhập lại mật khẩu mới")]
    public string ConfirmPassword { get; set; } = "";
}
