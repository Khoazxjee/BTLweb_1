using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace AuroraHotel.Tests;
[Collection("Hotel SQL Server")]
public class HttpTests(HotelFixture f)
{
    [Fact]
    public async Task Sql_schema_and_seed_scripts_are_repeatable_and_create_valid_folio()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder != null && !File.Exists(Path.Combine(folder.FullName, "AuroraHotel.sln")))
            folder = folder.Parent;
        Assert.NotNull(folder);
        var schema = await File.ReadAllTextAsync(Path.Combine(folder!.FullName, "docs", "schema.sql"));
        var seed = await File.ReadAllTextAsync(Path.Combine(folder.FullName, "docs", "seed-demo.sql"));
        await f.InScope(async sp =>
        {
            var db = sp.GetRequiredService<HotelDbContext>();
            var manager = sp.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = "admin@aurora.local",
                Email = "admin@aurora.local",
                FullName = "SQL test admin"
            };
            Assert.True((await manager.CreateAsync(user, f.Password)).Succeeded);
            await db.Database.OpenConnectionAsync();
            foreach (var batch in System.Text.RegularExpressions.Regex.Split(schema, @"^GO\s*$", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                if (!string.IsNullOrWhiteSpace(batch))
                    await db.Database.ExecuteSqlRawAsync(batch);
            await db.Database.ExecuteSqlRawAsync(seed);
            var before = await db.Reservations.CountAsync(x => x.ConfirmationCode.StartsWith("AU-SQL-"));
            Assert.Equal(3, before);
            await db.Database.ExecuteSqlRawAsync(seed);
            Assert.Equal(before, await db.Reservations.CountAsync(x => x.ConfirmationCode.StartsWith("AU-SQL-")));
            var folio = await db.Folios.Include(x => x.Items).SingleAsync(x => x.Reservation.ConfirmationCode == "AU-SQL-INHOUSE");
            Assert.Equal(1800000, folio.Total);
            await db.Database.CloseConnectionAsync();
            return true;
        });
    }

    [Fact]
    public async Task Search_paging_filters_and_safe_back_link_preserve_criteria()
    {
        var room = await f.NewRoom();
        var roomType = await f.InScope(async sp => (await sp.GetRequiredService<HotelDbContext>().Rooms.FindAsync(room))!.RoomTypeId);
        using var client = f.Client();
        var response = await client.GetAsync($"/api/rooms/availability?ArrivalDate=2030-05-12&DepartureDate=2030-05-14&GuestCount=2&RoomTypeId={roomType}&MinPrice=700000&MaxPrice=900000&Page=999");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, data.GetProperty("total").GetInt32());
        Assert.Equal(1, data.GetProperty("page").GetInt32());
        Assert.Equal(room, data.GetProperty("items")[0].GetProperty("id").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/rooms/availability?ArrivalDate=2030-05-14&DepartureDate=2030-05-12")).StatusCode);
        var back = "/Rooms?MaxPrice=900000&Sort=price-desc&page=2";
        var html = await client.GetStringAsync($"/Rooms/Details/{room}?returnUrl={Uri.EscapeDataString(back)}");
        Assert.Contains("MaxPrice=900000&amp;Sort=price-desc&amp;page=2", html);
        var hostile = await client.GetStringAsync($"/Rooms/Details/{room}?returnUrl={Uri.EscapeDataString("https://example.org/external")}");
        Assert.DoesNotContain("example.org", hostile);
    }

    [Fact]
    public async Task Every_main_page_renders_and_role_boundaries_are_enforced()
    {
        using var visitor = f.Client();
        foreach (var path in new[]
        {
            "/",
            "/Rooms",
            "/Account/Login",
            "/Account/Register"
        }

        )
            Assert.Equal(HttpStatusCode.OK, (await visitor.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await visitor.GetAsync("/Staff/Board")).StatusCode);
        using var customer = f.Client();
        await f.Login(customer, "customer");
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/services")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await customer.GetAsync("/Staff/Reservations")).StatusCode);
        foreach (var path in new[]
        {
            "/Bookings",
            "/Account/Profile"
        }

        )
            Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync(path)).StatusCode);
        using var staff = f.Client();
        await f.Login(staff, "staff");
        foreach (var path in new[]
        {
            "/Staff/FrontDesk",
            "/Staff/Board",
            "/Staff/Reservations",
            "/Staff/Reservations/Create",
            "/Staff/Guests",
            "/Staff/Guests/Edit"
        }

        )
            Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await staff.GetAsync("/Staff/Users")).StatusCode);
        using var admin = f.Client();
        await f.Login(admin, "admin");
        foreach (var path in new[]
        {
            "/Staff/Dashboard",
            "/Staff/Catalog/Rooms",
            "/Staff/Catalog/Room",
            "/Staff/Catalog/Types",
            "/Staff/Catalog/Type",
            "/Staff/Catalog/Services",
            "/Staff/Catalog/Service",
            "/Staff/Users"
        }

        )
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Ownership_and_csrf_protect_direct_URLs_and_posts()
    {
        var room = await f.NewRoom();
        var id = await f.Reservations(s => s.Create(new BookingInput { RoomId = room, GuestProfileId = f.GuestId, ArrivalDate = f.Clock.Today.AddDays(1), DepartureDate = f.Clock.Today.AddDays(3), GuestCount = 2 }, f.CustomerId, f.CustomerId));
        using var other = f.Client();
        await f.Login(other, "other");
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/Bookings/Details/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await f.Post(other, $"/Bookings/Cancel/{id}")).StatusCode);
        using var customer = f.Client();
        await f.Login(customer, "customer");
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync($"/Bookings/Details/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await customer.PostAsync($"/Bookings/Cancel/{id}", new StringContent(""))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await f.Client().GetAsync("/api/rooms/availability?ArrivalDate=2030-05-12&DepartureDate=2030-05-14&GuestCount=2")).StatusCode);
    }

    [Fact]
    public async Task Anonymous_selection_survives_login_and_booking_uses_server_price()
    {
        var room = await f.NewRoom();
        using var client = f.Client();
        var selected = await f.Post(client, "/Bookings/Select", new() { ["RoomId"] = room.ToString(), ["ArrivalDate"] = "2030-05-12", ["DepartureDate"] = "2030-05-14", ["GuestCount"] = "2" });
        Assert.Equal(HttpStatusCode.Redirect, selected.StatusCode);
        await f.Login(client, "customer");
        var html = await client.GetStringAsync("/Bookings/Create");
        Assert.Contains("800.000", html);
        var created = await f.Post(client, "/Bookings/Create", new() { ["RoomId"] = room.ToString(), ["ArrivalDate"] = "2030-05-12", ["DepartureDate"] = "2030-05-14", ["GuestCount"] = "2", ["NightlyRateSnapshot"] = "1", ["GuestProfileId"] = f.OtherGuestId.ToString() }, "/Bookings/Create");
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        await f.InScope(async sp =>
        {
            var booking = await sp.GetRequiredService<HotelDbContext>().Reservations.SingleAsync(x => x.RoomId == room);
            Assert.Equal(800000, booking.NightlyRateSnapshot);
            Assert.Equal(f.GuestId, booking.GuestProfileId);
            Assert.Equal(ReservationStatus.Pending, booking.Status);
            return true;
        });
    }

    [Fact]
    public async Task Service_REST_CRUD_has_csrf_admin_auth_validation_and_real_persistence()
    {
        using var admin = f.Client();
        await f.Login(admin, "admin");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/services", new { name = "Missing CSRF", unit = "lần", unitPrice = 100000 })).StatusCode);
        admin.DefaultRequestHeaders.Add("X-CSRF-TOKEN", await f.Token(admin));
        var response = await admin.PostAsJsonAsync("/api/services", new { name = "API kiểm thử", unit = "lần", unitPrice = 100000, isActive = true });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = json.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/services/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/services/{id}", new { name = "API updated", unit = "lần", unitPrice = 150000, isActive = true })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/services/{id}", new { name = "", unit = "lần", unitPrice = -1 })).StatusCode);
        using var staff = f.Client();
        await f.Login(staff, "staff");
        staff.DefaultRequestHeaders.Add("X-CSRF-TOKEN", await f.Token(staff));
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.DeleteAsync($"/api/services/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/services/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/services/{id}")).StatusCode);
    }

    [Fact]
    public async Task Guest_contact_email_does_not_change_identity_or_link_other_profile()
    {
        using var customer = f.Client();
        await f.Login(customer, "customer");
        var updated = await f.Post(customer, "/Account/Profile", new() { ["Id"] = f.OtherGuestId.ToString(), ["FullName"] = "Khách kiểm thử", ["Phone"] = "0901234567", ["Email"] = "other@test.local" });
        Assert.Equal(HttpStatusCode.Redirect, updated.StatusCode);
        await f.InScope(async sp =>
        {
            var db = sp.GetRequiredService<HotelDbContext>();
            var own = await db.GuestProfiles.FindAsync(f.GuestId);
            Assert.Equal("other@test.local", own!.Email);
            Assert.Equal(f.CustomerId, own.UserId);
            Assert.Equal("customer@test.local", (await db.Users.FindAsync(f.CustomerId))!.Email);
            Assert.Equal("other", (await db.GuestProfiles.FindAsync(f.OtherGuestId))!.FullName);
            return true;
        });
    }

    [Fact]
    public async Task Upload_decodes_images_rejects_disguised_content_and_limits_size()
    {
        await f.InScope(async sp =>
        {
            var catalog = sp.GetRequiredService<RoomImageService>();
            using var badStream = new MemoryStream(Encoding.UTF8.GetBytes("<script>alert(1)</script>"));
            var bad = new FormFile(badStream, 0, badStream.Length, "Image", "photo.jpg");
            await Assert.ThrowsAsync<BusinessException>(() => catalog.Upload(bad));
            using var bitmap = new SKBitmap(30, 30);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(data.ToArray());
            var good = new FormFile(stream, 0, stream.Length, "Image", "../../image.png");
            var result = await catalog.Upload(good);
            Assert.StartsWith("/uploads/", result);
            Assert.EndsWith(".webp", result);
            Assert.DoesNotContain("..", result);
            var large = new FormFile(stream, 0, 6 * 1024 * 1024, "Image", "too-big.png");
            await Assert.ThrowsAsync<BusinessException>(() => catalog.Upload(large));
            return true;
        });
    }

    [Fact]
    public async Task Admin_self_lock_blocked_and_user_cannot_register_as_admin()
    {
        using var admin = f.Client();
        await f.Login(admin, "admin");
        var result = await f.Post(admin, "/Staff/Users/Update", new() { ["id"] = f.AdminId, ["role"] = "Customer", ["locked"] = "true" });
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        await f.InScope(async sp =>
        {
            var manager = sp.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
            var user = await manager.FindByIdAsync(f.AdminId);
            Assert.True(await manager.IsInRoleAsync(user!, Roles.Admin));
            Assert.Null(user!.LockoutEnd);
            return true;
        });
        using var visitor = f.Client();
        var email = "registration-" + Guid.NewGuid().ToString("N") + "@test.local";
        var registered = await f.Post(visitor, "/Account/Register", new() { ["Email"] = email, ["FullName"] = "Khách đăng ký", ["Phone"] = "0901234567", ["Password"] = f.Password, ["ConfirmPassword"] = f.Password, ["Role"] = "Admin", ["ReturnUrl"] = "https://example.org" });
        Assert.Equal(HttpStatusCode.Redirect, registered.StatusCode);
        Assert.DoesNotContain("example.org", registered.Headers.Location!.ToString());
        await f.InScope(async sp =>
        {
            var manager = sp.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
            var user = await manager.FindByEmailAsync(email);
            Assert.True(await manager.IsInRoleAsync(user!, Roles.Customer));
            Assert.False(await manager.IsInRoleAsync(user!, Roles.Admin));
            Assert.Equal(1, await sp.GetRequiredService<HotelDbContext>().GuestProfiles.CountAsync(x => x.UserId == user!.Id));
            return true;
        });
    }
}
