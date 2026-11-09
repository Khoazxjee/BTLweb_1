using System.Security.Cryptography;
using AuroraHotel.Models;
using AuroraHotel.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Data;

/// <summary>
/// Khởi tạo dữ liệu mẫu (Seed Data) cho ứng dụng trong môi trường phát triển (Development).
/// Tạo các vai trò, tài khoản demo, danh mục phòng và đơn đặt phòng minh họa.
/// </summary>
public class SeedData
{
    private readonly HotelDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;
    private readonly IHotelClock _clock;
    private readonly IWebHostEnvironment _env;

    public SeedData(HotelDbContext db, UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles, IHotelClock clock, IWebHostEnvironment env)
    {
        _db = db;
        _users = users;
        _roles = roles;
        _clock = clock;
        _env = env;
    }

    public async Task Run()
    {
        if (!_env.IsDevelopment())
            return;

        var folder = Path.Combine(_env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(folder);

        // Mật khẩu demo dễ nhớ, chuẩn sinh viên làm đồ án
        const string password = "Admin@123";

        var passwordFile = Path.Combine(folder, "demo-password.txt");
        await File.WriteAllTextAsync(passwordFile, password);

        await Seed(password, false);

        await File.WriteAllLinesAsync(Path.Combine(folder, "demo-accounts.txt"), new[]
        {
            "TÀI KHOẢN DEMO ĐỒ ÁN AURORA HOTEL",
            "Mật khẩu chung cho tất cả tài khoản: " + password,
            "--------------------------------------------------",
            "admin@aurora.local   | Quản trị viên (Admin)",
            "letan@aurora.local   | Nhân viên lễ tân (Receptionist)",
            "minh@aurora.local    | Khách hàng (Customer)",
            "linh@aurora.local    | Khách hàng (Customer)"
        });
    }

    public async Task<IReadOnlyDictionary<string, string>> InitializeHostedDemo(string expectedDatabase, string password)
    {
        if (string.IsNullOrWhiteSpace(expectedDatabase) || !string.Equals(_db.Database.GetDbConnection().Database, expectedDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException("Tên database xác nhận không khớp connection string.");

        await _db.Database.MigrateAsync();
        var credentials = new Dictionary<string, string>
        {
            ["admin@aurora.local"] = password
        };
        foreach (var email in new[] { "letan@aurora.local", "minh@aurora.local", "linh@aurora.local" })
            credentials[email] = "Admin@123";

        await Seed(password, true, credentials);
        return credentials;
    }

    private async Task Seed(string password, bool requireEmpty, IReadOnlyDictionary<string, string>? credentials = null)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        if (requireEmpty && (await _db.Users.AnyAsync() || await _db.Roles.AnyAsync() || await _db.Rooms.AnyAsync() || await _db.GuestProfiles.AnyAsync()))
            throw new InvalidOperationException("Database đã có dữ liệu.");

        // 1. Tạo các vai trò nếu chưa tồn tại
        var roleNames = new[] { Roles.Admin, Roles.Receptionist, Roles.Customer };
        foreach (var role in roleNames)
        {
            if (!await _roles.RoleExistsAsync(role))
            {
                var roleResult = await _roles.CreateAsync(new IdentityRole(role));
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(x => x.Description)));
            }
        }

        // 2. Tạo hoặc cập nhật các tài khoản demo
        var demo = new[]
        {
            ("admin@aurora.local", "Quản trị Aurora", Roles.Admin),
            ("letan@aurora.local", "Lễ tân Aurora", Roles.Receptionist),
            ("minh@aurora.local", "Nguyễn An Minh", Roles.Customer),
            ("linh@aurora.local", "Trần Ngọc Linh", Roles.Customer)
        };

        foreach (var (email, name, role) in demo)
        {
            var user = await _users.FindByEmailAsync(email);
            var passToUse = credentials != null && credentials.ContainsKey(email) ? credentials[email] : password;

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = name,
                    EmailConfirmed = true
                };
                var result = await _users.CreateAsync(user, passToUse);
                if (!result.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));

                result = await _users.AddToRoleAsync(user, role);
                if (!result.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));

                if (role == Roles.Customer)
                    _db.GuestProfiles.Add(new GuestProfile { UserId = user.Id, FullName = name, Email = email, Phone = "0900000000" });

                await _db.SaveChangesAsync();
            }
            else
            {
                // Cập nhật lại mật khẩu thành Admin@123 và mở khóa tài khoản nếu đang khóa
                var resetToken = await _users.GeneratePasswordResetTokenAsync(user);
                await _users.ResetPasswordAsync(user, resetToken, passToUse);
                await _users.SetLockoutEndDateAsync(user, null);
            }
        }

        // Nếu đã có danh mục phòng thì không nạp lại
        if (await _db.Rooms.AnyAsync())
        {
            await tx.CommitAsync();
            return;
        }

        // 3. Khởi tạo Loại phòng
        var deluxe = new RoomType
        {
            Name = "Deluxe Garden",
            Capacity = 2,
            Description = "Khoảng nghỉ dịu dàng giữa sắc xanh."
        };
        var premier = new RoomType
        {
            Name = "Premier Ocean",
            Capacity = 3,
            Description = "Đón bình minh từ ban công riêng."
        };
        var suite = new RoomType
        {
            Name = "Aurora Suite",
            Capacity = 4,
            Description = "Không gian rộng rãi để tận hưởng cùng người thân."
        };
        _db.RoomTypes.AddRange(deluxe, premier, suite);

        // 4. Khởi tạo 12 phòng
        for (var i = 1; i <= 12; i++)
        {
            var type = i <= 5 ? deluxe : i <= 9 ? premier : suite;
            _db.Rooms.Add(new Room
            {
                RoomNumber = i <= 5 ? (100 + i).ToString() : i <= 9 ? (200 + i - 5).ToString() : (300 + i - 9).ToString(),
                RoomType = type,
                NightlyRate = type == deluxe ? 800000 : type == premier ? 1200000 : 2000000,
                Amenities = "Wi-Fi miễn phí, Điều hòa, Bồn tắm, Bữa sáng tùy chọn, Ban công",
                Description = "<p>Một không gian nghỉ ngơi ấm áp với ánh sáng tự nhiên, nội thất gỗ và những chi tiết được chăm chút.</p><ul><li>Giường ngủ êm ái và bàn làm việc riêng</li><li>Phòng tắm với vòi sen và đồ dùng cá nhân</li><li>Wi-Fi, điều hòa và nước uống trong phòng</li></ul>",
                ImagePath = type == deluxe ? "/images/room-deluxe.jpg" : type == premier ? "/images/room-premier.jpg" : "/images/room-suite.jpg",
                HousekeepingStatus = i == 4 ? HousekeepingStatus.Dirty : HousekeepingStatus.Clean,
                OperationalStatus = i == 12 ? OperationalStatus.OutOfOrder : OperationalStatus.Active
            });
        }

        // 5. Khởi tạo Dịch vụ
        _db.Services.AddRange(
            new HotelService { Name = "Bữa sáng tại nhà hàng", Unit = "suất", UnitPrice = 100000 },
            new HotelService { Name = "Giặt ủi", Unit = "kg", UnitPrice = 60000 },
            new HotelService { Name = "Đưa đón sân bay", Unit = "chuyến", UnitPrice = 250000 },
            new HotelService { Name = "Trà chiều", Unit = "phần", UnitPrice = 150000 }
        );

        await _db.SaveChangesAsync();

        var adminUser = (await _users.FindByEmailAsync("admin@aurora.local"))!;
        var guests = await _db.GuestProfiles.OrderBy(x => x.Id).ToListAsync();
        var rooms = await _db.Rooms.OrderBy(x => x.RoomNumber).ToListAsync();

        Reservation Make(string code, int room, int guest, DateOnly start, ReservationStatus status) => new()
        {
            ConfirmationCode = code,
            RoomId = rooms[room].Id,
            GuestProfileId = guests[guest].Id,
            ArrivalDate = start,
            DepartureDate = start.AddDays(2),
            GuestCount = 2,
            NightlyRateSnapshot = rooms[room].NightlyRate,
            Status = status,
            Source = ReservationSource.FrontDesk,
            CreatedByUserId = adminUser.Id,
            CreatedAt = _clock.Now,
            Notes = "Dữ liệu minh họa cho đồ án."
        };

        var today = _clock.Today;
        _db.Reservations.AddRange(
            Make("AU-DEMO-ARRIVAL", 0, 0, today, ReservationStatus.Confirmed),
            Make("AU-DEMO-PENDING", 1, 1, today, ReservationStatus.Pending),
            Make("AU-DEMO-FUTURE", 2, 0, today.AddDays(3), ReservationStatus.Confirmed)
        );

        var stay = Make("AU-DEMO-INHOUSE", 5, 1, today.AddDays(-1), ReservationStatus.CheckedIn);
        stay.CheckInAt = _clock.Now.AddDays(-1);
        stay.Folio = new Folio
        {
            CreatedAt = _clock.Now.AddDays(-1),
            Items = new List<FolioItem>
            {
                new FolioItem
                {
                    Kind = ItemKind.Room,
                    DescriptionSnapshot = "Tiền phòng 201",
                    Quantity = 2,
                    UnitPriceSnapshot = stay.NightlyRateSnapshot
                }
            }
        };
        _db.Reservations.Add(stay);

        var past = Make("AU-DEMO-COMPLETE", 6, 0, today.AddDays(-4), ReservationStatus.CheckedOut);
        past.CheckInAt = _clock.Now.AddDays(-4);
        past.CheckOutAt = _clock.Now.AddDays(-2);
        past.Folio = new Folio
        {
            CreatedAt = _clock.Now.AddDays(-4),
            ClosedAt = _clock.Now.AddDays(-2),
            Status = FolioStatus.Closed,
            Items = new List<FolioItem>
            {
                new FolioItem
                {
                    Kind = ItemKind.Room,
                    DescriptionSnapshot = "Tiền phòng 202",
                    Quantity = 2,
                    UnitPriceSnapshot = past.NightlyRateSnapshot
                }
            },
            Payment = new Payment
            {
                Amount = 2400000,
                Method = PaymentMethod.BankTransfer,
                PaidAt = _clock.Now.AddDays(-2),
                ReceivedByUserId = adminUser.Id
            }
        };
        _db.Reservations.Add(past);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
