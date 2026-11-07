using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AuroraHotel.Tests;
[CollectionDefinition("Hotel SQL Server", DisableParallelization = true)]
public class HotelCollection : ICollectionFixture<HotelFixture>;
public class TestClock : IHotelClock
{
    public DateTimeOffset Now => new(2030, 5, 12, 10, 0, 0, TimeSpan.FromHours(7));
    public DateOnly Today => new(2030, 5, 12);
}

public class HotelFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string database = "AuroraHotel_Tests_" + Guid.NewGuid().ToString("N");
    public string Password { get; } = "Test!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(10)) + "a1";
    public string AdminId { get; private set; } = "";
    public string StaffId { get; private set; } = "";
    public string CustomerId { get; private set; } = "";
    public string OtherId { get; private set; } = "";
    public int GuestId { get; private set; }
    public int OtherGuestId { get; private set; }
    public TestClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<HotelDbContext>>();
            services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<HotelDbContext>>();
            services.AddDbContext<HotelDbContext>(o => o.UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={database};Trusted_Connection=True;TrustServerCertificate=True"));
            services.RemoveAll<IHotelClock>();
            services.AddSingleton<IHotelClock>(Clock);
        });
    }

    public async Task InitializeAsync()
    {
        await InScope(async sp =>
        {
            var db = sp.GetRequiredService<HotelDbContext>();
            await db.Database.MigrateAsync();
            var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var role in new[]
            {
                Roles.Admin,
                Roles.Receptionist,
                Roles.Customer
            }

            )
                Assert.True((await roles.CreateAsync(new IdentityRole(role))).Succeeded);
            var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
            foreach (var(name, role)in new[]
            {
                ("admin", Roles.Admin),
                ("staff", Roles.Receptionist),
                ("customer", Roles.Customer),
                ("other", Roles.Customer)
            }

            )
            {
                var user = new ApplicationUser
                {
                    UserName = name + "@test.local",
                    Email = name + "@test.local",
                    FullName = name
                };
                Assert.True((await users.CreateAsync(user, Password)).Succeeded);
                Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
                if (name == "admin")
                    AdminId = user.Id;
                if (name == "staff")
                    StaffId = user.Id;
                if (role == Roles.Customer)
                {
                    var guest = new GuestProfile
                    {
                        UserId = user.Id,
                        FullName = name,
                        Email = user.Email,
                        Phone = "0900000000"
                    };
                    db.GuestProfiles.Add(guest);
                    await db.SaveChangesAsync();
                    if (name == "customer")
                    {
                        CustomerId = user.Id;
                        GuestId = guest.Id;
                    }
                    else
                    {
                        OtherId = user.Id;
                        OtherGuestId = guest.Id;
                    }
                }
            }

            return true;
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        // Delete only the isolated database created by this fixture, never Development or a user database.
        if (!database.StartsWith("AuroraHotel_Tests_", StringComparison.Ordinal))
            throw new InvalidOperationException();
        await InScope(async sp =>
        {
            await sp.GetRequiredService<HotelDbContext>().Database.EnsureDeletedAsync();
            return true;
        });
        await base.DisposeAsync();
    }

    public async Task<T> InScope<T>(Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    public Task<T> Reservations<T>(Func<ReservationService, Task<T>> action) => InScope(sp => action(sp.GetRequiredService<ReservationService>()));
    public Task Reservations(Func<ReservationService, Task> action) => InScope(async sp =>
    {
        await action(sp.GetRequiredService<ReservationService>());
        return true;
    });
    public Task<T> FrontDesk<T>(Func<FrontDeskService, Task<T>> action) => InScope(sp => action(sp.GetRequiredService<FrontDeskService>()));
    public Task FrontDesk(Func<FrontDeskService, Task> action) => InScope(async sp =>
    {
        await action(sp.GetRequiredService<FrontDeskService>());
        return true;
    });
    public Task<T> Availability<T>(Func<RoomAvailabilityService, Task<T>> action) => InScope(sp => action(sp.GetRequiredService<RoomAvailabilityService>()));
    public Task Availability(Func<RoomAvailabilityService, Task> action) => InScope(async sp =>
    {
        await action(sp.GetRequiredService<RoomAvailabilityService>());
        return true;
    });
    public Task<T> Rooms<T>(Func<RoomStatusService, Task<T>> action) => InScope(sp => action(sp.GetRequiredService<RoomStatusService>()));
    public Task Rooms(Func<RoomStatusService, Task> action) => InScope(async sp =>
    {
        await action(sp.GetRequiredService<RoomStatusService>());
        return true;
    });
    public Task<int> NewRoom(HousekeepingStatus housekeeping = HousekeepingStatus.Clean, OperationalStatus operation = OperationalStatus.Active) => InScope(async sp =>
    {
        var db = sp.GetRequiredService<HotelDbContext>();
        var type = new RoomType
        {
            Name = "Test " + Guid.NewGuid().ToString("N")[..8],
            Capacity = 2
        };
        var room = new Room
        {
            RoomNumber = "T" + Guid.NewGuid().ToString("N")[..12],
            RoomType = type,
            NightlyRate = 800000,
            HousekeepingStatus = housekeeping,
            OperationalStatus = operation
        };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return room.Id;
    });
    public Task<int> NewService() => InScope(async sp =>
    {
        var db = sp.GetRequiredService<HotelDbContext>();
        var service = new HotelService
        {
            Name = "Dịch vụ kiểm thử",
            Unit = "lần",
            UnitPrice = 100000
        };
        db.Services.Add(service);
        await db.SaveChangesAsync();
        return service.Id;
    });
    public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
    public async Task<string> Token(HttpClient client, string path = "/Account/Login")
    {
        var html = await client.GetStringAsync(path);
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "Antiforgery token not found on " + path);
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    public async Task Login(HttpClient client, string name)
    {
        var token = await Token(client);
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string> { ["Email"] = name + "@test.local", ["Password"] = Password, ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    public async Task<HttpResponseMessage> Post(HttpClient client, string path, Dictionary<string, string>? values = null, string tokenPage = "/Account/Login")
    {
        values ??= [];
        values["__RequestVerificationToken"] = await Token(client, tokenPage);
        return await client.PostAsync(path, new FormUrlEncodedContent(values));
    }
}
