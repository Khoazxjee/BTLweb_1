using System.Security.Claims;
using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Controllers;

/// <summary>
/// Controller quản lý tài khoản người dùng: Đăng nhập, đăng ký, đăng xuất, đổi mật khẩu và xem thông tin cá nhân.
/// </summary>
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly HotelDbContext _db;

    public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, HotelDbContext db)
    {
        _users = users;
        _signIn = signIn;
        _db = db;
    }

    // GET: /Account/Login - Hiển thị form đăng nhập
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        return View(new LoginInput { ReturnUrl = returnUrl });
    }

    // POST: /Account/Login - Xử lý đăng nhập
    [HttpPost]
    public async Task<IActionResult> Login(LoginInput input)
    {
        if (!ModelState.IsValid)
            return View(input);

        // 1. Tìm user theo Email
        var user = await _users.FindByEmailAsync(input.Email.Trim());

        // 2. Xác thực mật khẩu
        var result = user == null
            ? Microsoft.AspNetCore.Identity.SignInResult.Failed
            : await _signIn.PasswordSignInAsync(user, input.Password, input.RememberMe, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng, hoặc tài khoản đang tạm khóa.");
            return View(input);
        }

        // 3. Điều hướng sau khi đăng nhập thành công
        if (input.ReturnUrl != null && Url.IsLocalUrl(input.ReturnUrl))
            return LocalRedirect(input.ReturnUrl);

        // Điều hướng theo vai trò (Role)
        if (await _users.IsInRoleAsync(user!, Roles.Admin))
            return RedirectToAction("Index", "Dashboard", new { area = "Staff" });

        if (await _users.IsInRoleAsync(user!, Roles.Receptionist))
            return RedirectToAction("Index", "FrontDesk", new { area = "Staff" });

        if (HttpContext.Session.GetString("BookingSelection") != null)
            return RedirectToAction("Create", "Bookings");

        return RedirectToAction("Index", "Home");
    }

    // GET: /Account/Register - Hiển thị form đăng ký
    [HttpGet]
    public IActionResult Register(string? returnUrl)
    {
        return View(new RegisterInput { ReturnUrl = returnUrl });
    }

    // POST: /Account/Register - Xử lý đăng ký tài khoản khách hàng mới
    [HttpPost]
    public async Task<IActionResult> Register(RegisterInput input)
    {
        if (!ModelState.IsValid)
            return View(input);

        await using var tx = await _db.Database.BeginTransactionAsync();

        // 1. Tạo tài khoản Identity
        var user = new ApplicationUser
        {
            UserName = input.Email.Trim(),
            Email = input.Email.Trim(),
            FullName = input.FullName.Trim()
        };

        var result = await _users.CreateAsync(user, input.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Code.Contains("Duplicate") ? "Email đã được đăng ký." : "Mật khẩu cần ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");
            return View(input);
        }

        // 2. Gán vai trò mặc định là Khách hàng (Customer)
        var roleResult = await _users.AddToRoleAsync(user, Roles.Customer);
        if (!roleResult.Succeeded)
            throw new InvalidOperationException("Không thể gán quyền khách hàng.");

        // 3. Tạo hồ sơ khách hàng tương ứng trong CSDL
        _db.GuestProfiles.Add(new GuestProfile
        {
            UserId = user.Id,
            FullName = input.FullName.Trim(),
            Email = input.Email.Trim(),
            Phone = input.Phone.Trim()
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        // 4. Tự động đăng nhập sau khi đăng ký
        await _signIn.SignInAsync(user, isPersistent: false);

        if (input.ReturnUrl != null && Url.IsLocalUrl(input.ReturnUrl))
            return LocalRedirect(input.ReturnUrl);

        return HttpContext.Session.GetString("BookingSelection") != null
            ? RedirectToAction("Create", "Bookings")
            : RedirectToAction("Index", "Home");
    }

    // POST: /Account/Logout - Đăng xuất
    [Authorize, HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    // GET: /Account/ChangePassword - Đổi mật khẩu
    [Authorize, HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordInput());

    // POST: /Account/ChangePassword - Xử lý đổi mật khẩu
    [Authorize, HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordInput input)
    {
        if (!ModelState.IsValid)
            return View(input);

        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var result = await _users.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Code == "PasswordMismatch" ? "Mật khẩu hiện tại chưa đúng." : "Mật khẩu mới cần có chữ hoa, chữ thường, số và ký tự đặc biệt.");
            return View(input);
        }

        await _signIn.RefreshSignInAsync(user);
        TempData["Success"] = "Đã đổi mật khẩu thành công!";
        return RedirectToAction(nameof(ChangePassword));
    }

    // Trang thông báo từ chối truy cập (403 Forbidden)
    public IActionResult Denied()
    {
        Response.StatusCode = 403;
        ViewBag.Code = 403;
        return View("~/Views/Home/Status.cshtml");
    }

    // GET: /Account/Profile - Xem thông tin cá nhân
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var guest = await _db.GuestProfiles.SingleAsync(x => x.UserId == userId);
        return View(new GuestInput
        {
            Id = guest.Id,
            FullName = guest.FullName,
            Phone = guest.Phone,
            Email = guest.Email
        });
    }

    // POST: /Account/Profile - Cập nhật thông tin cá nhân
    [Authorize(Roles = Roles.Customer), HttpPost]
    public async Task<IActionResult> Profile(GuestInput input)
    {
        if (!ModelState.IsValid)
            return View(input);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var guest = await _db.GuestProfiles.SingleAsync(x => x.UserId == userId);

        guest.FullName = input.FullName.Trim();
        guest.Phone = input.Phone.Trim();
        guest.Email = input.Email.Trim();

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật thông tin liên hệ thành công!";
        return RedirectToAction(nameof(Profile));
    }
}
