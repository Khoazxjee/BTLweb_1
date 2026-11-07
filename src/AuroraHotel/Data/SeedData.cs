using System.Security.Cryptography;
using AuroraHotel.Models;
using AuroraHotel.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Data;

public class SeedData(HotelDbContext db, UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles, IHotelClock clock, IWebHostEnvironment env)
{
    public async Task Run()
    {
        if (!env.IsDevelopment())
            return;
        var folder = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(folder);
        var passwordFile = Path.Combine(folder, "demo-password.txt");
        var password = File.Exists(passwordFile) ? (await File.ReadAllTextAsync(passwordFile)).Trim() : "Aurora!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(9)) + "a1";
        if (!File.Exists(passwordFile))
            await File.WriteAllTextAsync(passwordFile, password);
        await Seed(password, false);
        await File.WriteAllLinesAsync(Path.Combine(folder, "demo-accounts.txt"), new[] { "TÀI KHOẢN DEMO CHỈ DÙNG DEVELOPMENT — không đưa file này lên Git", "Mật khẩu: " + password, "admin@aurora.local | Admin", "letan@aurora.local | Receptionist", "minh@aurora.local | Customer", "linh@aurora.local | Customer" });
    }

    // Called only by the explicit offline deployment command, never by an HTTP request.
    public async Task<IReadOnlyDictionary<string, string>> InitializeHostedDemo(string expectedDatabase, string password)
    {
        if (string.IsNullOrWhiteSpace(expectedDatabase) || !string.Equals(db.Database.GetDbConnection().Database, expectedDatabase, StringComparison.Ordinal))
            throw new InvalidOperationException("Tên database xác nhận không khớp connection string. Chưa thay đổi dữ liệu.");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 16 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || !password.Any(c => !char.IsLetterOrDigit(c)))
            throw new InvalidOperationException("Mật khẩu demo hosting cần ít nhất 16 ký tự, gồm chữ hoa, chữ thường, số và ký hiệu.");
        await db.Database.MigrateAsync();
        var credentials = new Dictionary<string, string>
        {
            ["admin@aurora.local"] = password
        };
        foreach (var email in new[]
        {
            "letan@aurora.local",
            "minh@aurora.local",
            "linh@aurora.local"
        }

        )
            credentials[email] = "Aurora!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)) + "a1";
        await Seed(password, true, credentials);
        return credentials;
    }

    private async Task Seed(string password, bool requireEmpty, IReadOnlyDictionary<string, string>? credentials = null)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource=N'AuroraHotel:operations',
                @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
            IF @result < 0 THROW 51000, 'Hotel is busy; retry setup.', 1;
            """);
        if (requireEmpty && (await db.Users.AnyAsync() || await db.Roles.AnyAsync() || await db.Rooms.AnyAsync() || await db.GuestProfiles.AnyAsync()))
            throw new InvalidOperationException("Database đã có dữ liệu. Lệnh khởi tạo chỉ dùng một lần cho database mới; không ghi đè hoặc đổi mật khẩu hiện có.");
        foreach (var role in new[]
        {
            Roles.Admin,
            Roles.Receptionist,
            Roles.Customer
        }

        )
        {
            if (await roles.RoleExistsAsync(role))
                continue;
            var result = await roles.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        var demo = new[]
        {
            ("admin@aurora.local", "Quản trị Aurora", Roles.Admin),
            ("letan@aurora.local", "Lễ tân Aurora", Roles.Receptionist),
            ("minh@aurora.local", "Nguyễn An Minh", Roles.Customer),
            ("linh@aurora.local", "Trần Ngọc Linh", Roles.Customer)
        };
        foreach (var(email, name, role)in demo)
        {
            var user = await users.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = name,
                    EmailConfirmed = true
                };
                var result = await users.CreateAsync(user, credentials == null ? password : credentials[email]);
                if (!result.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
                result = await users.AddToRoleAsync(user, role);
                if (!result.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
                if (role == Roles.Customer)
                    db.GuestProfiles.Add(new GuestProfile { UserId = user.Id, FullName = name, Email = email, Phone = "0900000000" });
                await db.SaveChangesAsync();
            }
        }

        if (await db.Rooms.AnyAsync())
        {
            await tx.CommitAsync();
            return;
        }

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
        db.RoomTypes.AddRange(deluxe, premier, suite);
        for (var i = 1; i <= 12; i++)
        {
            var type = i <= 5 ? deluxe : i <= 9 ? premier : suite;
            db.Rooms.Add(new Room { RoomNumber = i <= 5 ? (100 + i).ToString() : i <= 9 ? (200 + i - 5).ToString() : (300 + i - 9).ToString(), RoomType = type, NightlyRate = type == deluxe ? 800000 : type == premier ? 1200000 : 2000000, Amenities = "Wi-Fi miễn phí, Điều hòa, Bồn tắm, Bữa sáng tùy chọn, Ban công", Description = "<p>Một không gian nghỉ ngơi ấm áp với ánh sáng tự nhiên, nội thất gỗ và những chi tiết được chăm chút.</p><ul><li>Giường ngủ êm ái và bàn làm việc riêng</li><li>Phòng tắm với vòi sen và đồ dùng cá nhân</li><li>Wi-Fi, điều hòa và nước uống trong phòng</li></ul>", ImagePath = type == deluxe ? "/images/room-deluxe.jpg" : type == premier ? "/images/room-premier.jpg" : "/images/room-suite.jpg", HousekeepingStatus = i == 4 ? HousekeepingStatus.Dirty : HousekeepingStatus.Clean, OperationalStatus = i == 12 ? OperationalStatus.OutOfOrder : OperationalStatus.Active });
        }

        db.Services.AddRange(new HotelService { Name = "Bữa sáng tại nhà hàng", Unit = "suất", UnitPrice = 100000 }, new HotelService { Name = "Giặt ủi", Unit = "kg", UnitPrice = 60000 }, new HotelService { Name = "Đưa đón sân bay", Unit = "chuyến", UnitPrice = 250000 }, new HotelService { Name = "Trà chiều", Unit = "phần", UnitPrice = 150000 });
        await db.SaveChangesAsync();
        var admin = (await users.FindByEmailAsync("admin@aurora.local"))!;
        var guests = await db.GuestProfiles.OrderBy(x => x.Id).ToListAsync();
        var rooms = await db.Rooms.OrderBy(x => x.RoomNumber).ToListAsync();
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
            CreatedByUserId = admin.Id,
            CreatedAt = clock.Now,
            Notes = "Dữ liệu minh họa cho đồ án."
        };
        var today = clock.Today;
        db.Reservations.AddRange(Make("AU-DEMO-ARRIVAL", 0, 0, today, ReservationStatus.Confirmed), Make("AU-DEMO-PENDING", 1, 1, today, ReservationStatus.Pending), Make("AU-DEMO-FUTURE", 2, 0, today.AddDays(3), ReservationStatus.Confirmed));
        var stay = Make("AU-DEMO-INHOUSE", 5, 1, today.AddDays(-1), ReservationStatus.CheckedIn);
        stay.CheckInAt = clock.Now.AddDays(-1);
        stay.Folio = new Folio
        {
            CreatedAt = clock.Now.AddDays(-1),
            Items = [new FolioItem
            {
                Kind = ItemKind.Room,
                DescriptionSnapshot = "Tiền phòng 201",
                Quantity = 2,
                UnitPriceSnapshot = stay.NightlyRateSnapshot
            }

            ]
        };
        db.Reservations.Add(stay);
        var past = Make("AU-DEMO-COMPLETE", 6, 0, today.AddDays(-4), ReservationStatus.CheckedOut);
        past.CheckInAt = clock.Now.AddDays(-4);
        past.CheckOutAt = clock.Now.AddDays(-2);
        past.Folio = new Folio
        {
            CreatedAt = clock.Now.AddDays(-4),
            ClosedAt = clock.Now.AddDays(-2),
            Status = FolioStatus.Closed,
            Items = [new FolioItem
            {
                Kind = ItemKind.Room,
                DescriptionSnapshot = "Tiền phòng 202",
                Quantity = 2,
                UnitPriceSnapshot = past.NightlyRateSnapshot
            }

            ],
            Payment = new Payment
            {
                Amount = 2400000,
                Method = PaymentMethod.BankTransfer,
                PaidAt = clock.Now.AddDays(-2),
                ReceivedByUserId = admin.Id
            }
        };
        db.Reservations.Add(past);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
