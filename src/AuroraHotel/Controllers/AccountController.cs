using System.Security.Claims;
using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Controllers;

public class AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, HotelDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl) => View(new LoginInput { ReturnUrl = returnUrl });
    [HttpPost]
    public async Task<IActionResult> Login(LoginInput input)
    {
        if (!ModelState.IsValid)
            return View(input);
        var user = await users.FindByEmailAsync(input.Email.Trim());
        var result = user == null ? Microsoft.AspNetCore.Identity.SignInResult.Failed : await signIn.PasswordSignInAsync(user, input.Password, input.RememberMe, true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng, hoặc tài khoản đang tạm khóa.");
            return View(input);
        }

        if (input.ReturnUrl != null && Url.IsLocalUrl(input.ReturnUrl))
            return LocalRedirect(input.ReturnUrl);
        if (await users.IsInRoleAsync(user!, Roles.Admin))
            return RedirectToAction("Index", "Dashboard", new { area = "Staff" });
        if (await users.IsInRoleAsync(user!, Roles.Receptionist))
            return RedirectToAction("Index", "FrontDesk", new { area = "Staff" });
        if (HttpContext.Session.GetString("BookingSelection") != null)
            return RedirectToAction("Create", "Bookings");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl) => View(new RegisterInput { ReturnUrl = returnUrl });
    [HttpPost]
    public async Task<IActionResult> Register(RegisterInput input)
    {
        if (!ModelState.IsValid)
            return View(input);
        await using var tx = await db.Database.BeginTransactionAsync();
        var user = new ApplicationUser
        {
            UserName = input.Email.Trim(),
            Email = input.Email.Trim(),
            FullName = input.FullName.Trim()
        };
        var result = await users.CreateAsync(user, input.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Code.Contains("Duplicate") ? "Email đã được đăng ký." : "Mật khẩu cần ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");
            return View(input);
        }

        var roleResult = await users.AddToRoleAsync(user, Roles.Customer);
        if (!roleResult.Succeeded)
            throw new InvalidOperationException("Unable to assign customer role.");
        db.GuestProfiles.Add(new GuestProfile { UserId = user.Id, FullName = input.FullName.Trim(), Email = input.Email.Trim(), Phone = input.Phone.Trim() });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        await signIn.SignInAsync(user, false);
        if (input.ReturnUrl != null && Url.IsLocalUrl(input.ReturnUrl))
            return LocalRedirect(input.ReturnUrl);
        return HttpContext.Session.GetString("BookingSelection") != null ? RedirectToAction("Create", "Bookings") : RedirectToAction("Index", "Home");
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    [Authorize, HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordInput());
    [Authorize, HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordInput input)
    {
        if (!ModelState.IsValid)
            return View(input);
        var user = await users.GetUserAsync(User);
        if (user == null)
            return Challenge();
        var result = await users.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Code == "PasswordMismatch" ? "Mật khẩu hiện tại chưa đúng." : "Mật khẩu mới cần có chữ hoa, chữ thường, số và ký tự đặc biệt.");
            return View(input);
        }

        await signIn.RefreshSignInAsync(user);
        TempData["Success"] = "Đã đổi mật khẩu. Các phiên đăng nhập khác sẽ hết hiệu lực.";
        return RedirectToAction(nameof(ChangePassword));
    }

    public IActionResult Denied()
    {
        Response.StatusCode = 403;
        ViewBag.Code = 403;
        return View("~/Views/Home/Status.cshtml");
    }

    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> Profile()
    {
        var guest = await db.GuestProfiles.SingleAsync(x => x.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier));
        return View(new GuestInput { Id = guest.Id, FullName = guest.FullName, Phone = guest.Phone, Email = guest.Email });
    }

    [Authorize(Roles = Roles.Customer), HttpPost]
    public async Task<IActionResult> Profile(GuestInput input)
    {
        if (!ModelState.IsValid)
            return View(input);
        var guest = await db.GuestProfiles.SingleAsync(x => x.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier));
        guest.FullName = input.FullName.Trim();
        guest.Phone = input.Phone.Trim();
        guest.Email = input.Email.Trim();
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật liên hệ. Email đăng nhập vẫn giữ nguyên.";
        return RedirectToAction(nameof(Profile));
    }
}
