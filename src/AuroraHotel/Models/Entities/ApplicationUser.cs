using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AuroraHotel.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string FullName { get; set; } = "";
}
