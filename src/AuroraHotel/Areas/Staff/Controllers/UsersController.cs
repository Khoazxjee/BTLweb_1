using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Areas.Staff.Controllers;

[Authorize(Roles = Roles.Admin)]
public class UsersController(HotelDbContext db, UserManager<ApplicationUser> users, HotelTransaction transaction, IHotelClock clock) : StaffController
{
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        var query = db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.FullName.Contains(q) || x.Email!.Contains(q));
        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (total + 11) / 12));
        var items = await query.OrderBy(x => x.Email).Skip((page - 1) * 12).Take(12).ToListAsync();
        var roleMap = new Dictionary<string, string>();
        foreach (var user in items)
            roleMap[user.Id] = string.Join(", ", await users.GetRolesAsync(user));
        ViewBag.UserRoles = roleMap;
        return View(new PageResult<ApplicationUser>(items, total, page, 12));
    }

    [HttpPost]
    public async Task<IActionResult> Update(string id, string role, bool locked)
    {
        await Run(() => transaction.RunAsync(async () =>
        {
            if (!new[]
            {
                Roles.Admin,
                Roles.Receptionist,
                Roles.Customer
            }.Contains(role))
                throw new BusinessException("Vai trò không hợp lệ.");
            var user = await users.FindByIdAsync(id) ?? throw new BusinessException("Không tìm thấy tài khoản.");
            if (id == Actor)
                throw new BusinessException("Không đổi quyền hoặc khóa tài khoản quản trị đang sử dụng.");
            var current = await users.GetRolesAsync(user);
            if (current.Contains(Roles.Admin) && (locked || role != Roles.Admin))
            {
                var admins = await users.GetUsersInRoleAsync(Roles.Admin);
                if (!admins.Any(x => x.Id != id && (!x.LockoutEnd.HasValue || x.LockoutEnd <= clock.Now)))
                    throw new BusinessException("Phải giữ ít nhất một quản trị viên đang hoạt động.");
            }

            Ensure(await users.RemoveFromRolesAsync(user, current));
            Ensure(await users.AddToRoleAsync(user, role));
            Ensure(await users.SetLockoutEnabledAsync(user, true));
            Ensure(await users.SetLockoutEndDateAsync(user, locked ? DateTimeOffset.MaxValue : null));
            Ensure(await users.UpdateSecurityStampAsync(user));
            if (role == Roles.Customer && !await db.GuestProfiles.AnyAsync(x => x.UserId == id))
                db.GuestProfiles.Add(new GuestProfile { UserId = id, FullName = user.FullName, Email = user.Email!, Phone = user.PhoneNumber ?? "" });
        }), "Đã cập nhật quyền và trạng thái tài khoản.");
        return RedirectToAction(nameof(Index));
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new BusinessException("Không thể cập nhật tài khoản. Vui lòng tải lại rồi thử lại.");
    }
}
