using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Areas.Staff.Controllers;

/// <summary>
/// Controller quản lý tài khoản người dùng, phân quyền và khóa tài khoản - Dành riêng cho Quản trị viên (Admin).
/// </summary>
[Authorize(Roles = Roles.Admin)]
public class UsersController : StaffController
{
    private readonly HotelDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly HotelTransaction _transaction;
    private readonly IHotelClock _clock;

    public UsersController(HotelDbContext db, UserManager<ApplicationUser> users, HotelTransaction transaction, IHotelClock clock)
    {
        _db = db;
        _users = users;
        _transaction = transaction;
        _clock = clock;
    }

    // GET: /Staff/Users - Danh sách tài khoản người dùng và vai trò
    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.FullName.Contains(q) || x.Email!.Contains(q));

        var total = await query.CountAsync();
        page = Math.Clamp(page, 1, Math.Max(1, (total + 11) / 12));

        var items = await query.OrderBy(x => x.Email).Skip((page - 1) * 12).Take(12).ToListAsync();

        // Lấy vai trò tương ứng của từng người dùng
        var roleMap = new Dictionary<string, string>();
        foreach (var user in items)
        {
            roleMap[user.Id] = string.Join(", ", await _users.GetRolesAsync(user));
        }

        ViewBag.UserRoles = roleMap;
        return View(new PageResult<ApplicationUser>(items, total, page, 12));
    }

    // POST: /Staff/Users/Update - Cập nhật vai trò (Role) hoặc Khóa/Mở khóa tài khoản
    [HttpPost]
    public async Task<IActionResult> Update(string id, string role, bool locked)
    {
        await Run(() => _transaction.RunAsync(async () =>
        {
            var validRoles = new[] { Roles.Admin, Roles.Receptionist, Roles.Customer };
            if (!validRoles.Contains(role))
                throw new BusinessException("Vai trò không hợp lệ.");

            var user = await _users.FindByIdAsync(id)
                ?? throw new BusinessException("Không tìm thấy tài khoản.");

            // Không cho phép tự khóa hoặc đổi quyền của chính tài khoản admin đang đăng nhập
            if (id == Actor)
                throw new BusinessException("Không đổi quyền hoặc khóa tài khoản quản trị đang sử dụng.");

            // Đảm bảo luôn còn ít nhất 1 admin hoạt động trong hệ thống
            var current = await _users.GetRolesAsync(user);
            if (current.Contains(Roles.Admin) && (locked || role != Roles.Admin))
            {
                var admins = await _users.GetUsersInRoleAsync(Roles.Admin);
                if (!admins.Any(x => x.Id != id && (!x.LockoutEnd.HasValue || x.LockoutEnd <= _clock.Now)))
                    throw new BusinessException("Phải giữ ít nhất một quản trị viên đang hoạt động.");
            }

            // Cập nhật vai trò
            Ensure(await _users.RemoveFromRolesAsync(user, current));
            Ensure(await _users.AddToRoleAsync(user, role));

            // Cập nhật trạng thái khóa
            Ensure(await _users.SetLockoutEnabledAsync(user, true));
            Ensure(await _users.SetLockoutEndDateAsync(user, locked ? DateTimeOffset.MaxValue : null));
            Ensure(await _users.UpdateSecurityStampAsync(user));

            if (role == Roles.Customer && !await _db.GuestProfiles.AnyAsync(x => x.UserId == id))
            {
                _db.GuestProfiles.Add(new GuestProfile
                {
                    UserId = id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    Phone = user.PhoneNumber ?? ""
                });
            }
        }), "Đã cập nhật quyền và trạng thái tài khoản thành công.");

        return RedirectToAction(nameof(Index));
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new BusinessException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }
}
