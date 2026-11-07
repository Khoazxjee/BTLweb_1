using System.Net;
using System.Text;
using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuroraHotel.Tests;
[Collection("Hotel SQL Server")]
public class ServerRenderedWorkflowTests(HotelFixture fixture)
{
    [Fact]
    public async Task Price_preview_recalculates_without_creating_a_reservation_or_loading_scripts()
    {
        using var client = fixture.Client();
        await fixture.Login(client, "customer");
        var roomId = await fixture.NewRoom();
        var response = await fixture.Post(client, "/Bookings/Create", new() { ["RoomId"] = roomId.ToString(), ["ArrivalDate"] = fixture.Clock.Today.AddDays(5).ToString("yyyy-MM-dd"), ["DepartureDate"] = fixture.Clock.Today.AddDays(8).ToString("yyyy-MM-dd"), ["GuestCount"] = "2", ["intent"] = "preview" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("2.400.000 VND", html);
        Assert.Contains("Đã kiểm tra lịch và tính lại tiền phòng", html);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.False(await fixture.InScope(sp => sp.GetRequiredService<HotelDbContext>().Reservations.AnyAsync(x => x.RoomId == roomId)));
    }

    [Fact]
    public async Task Confirm_controls_render_as_native_details_and_keep_antiforgery_protection()
    {
        var room = await fixture.NewRoom();
        var booking = await fixture.Reservations(service => service.Create(new BookingInput { RoomId = room, GuestProfileId = fixture.GuestId, GuestCount = 2, ArrivalDate = fixture.Clock.Today.AddDays(4), DepartureDate = fixture.Clock.Today.AddDays(6) }, fixture.CustomerId, fixture.CustomerId));
        using var client = fixture.Client();
        await fixture.Login(client, "staff");
        var html = await client.GetStringAsync($"/Staff/Reservations/Details/{booking}");
        Assert.Contains("<details class=\"confirm-action\">", html);
        Assert.DoesNotContain("<confirm-submit", html);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        var missingToken = await client.PostAsync($"/Staff/Reservations/Confirm/{booking}", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, missingToken.StatusCode);
        var confirmed = await fixture.Post(client, $"/Staff/Reservations/Confirm/{booking}");
        Assert.Equal(HttpStatusCode.Redirect, confirmed.StatusCode);
        Assert.Equal(ReservationStatus.Confirmed, await fixture.InScope(sp => sp.GetRequiredService<HotelDbContext>().Reservations.Where(x => x.Id == booking).Select(x => x.Status).SingleAsync()));
    }

    [Fact]
    public async Task Report_export_contains_paid_rows_only_and_escapes_spreadsheet_formulas()
    {
        var guestId = await fixture.InScope(async sp =>
        {
            var db = sp.GetRequiredService<HotelDbContext>();
            var guest = new GuestProfile
            {
                FullName = "=2+2",
                Phone = "0900000000",
                Email = "csv@test.local"
            };
            db.GuestProfiles.Add(guest);
            await db.SaveChangesAsync();
            return guest.Id;
        });
        var room = await fixture.NewRoom();
        var reservation = await fixture.Reservations(service => service.Create(new BookingInput { RoomId = room, GuestProfileId = guestId, GuestCount = 1, ArrivalDate = fixture.Clock.Today, DepartureDate = fixture.Clock.Today.AddDays(2) }, fixture.StaffId));
        await fixture.Reservations(service => service.Confirm(reservation));
        var folio = await fixture.FrontDesk(service => service.CheckIn(reservation));
        await fixture.FrontDesk(service => service.Pay(folio, new PaymentInput { Amount = 1600000 }, fixture.StaffId));
        var code = await fixture.InScope(sp => sp.GetRequiredService<HotelDbContext>().Reservations.Where(x => x.Id == reservation).Select(x => x.ConfirmationCode).SingleAsync());
        using var admin = fixture.Client();
        await fixture.Login(admin, "admin");
        var response = await admin.GetAsync("/Staff/Dashboard/Export?from=2030-05-12&to=2030-05-12");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));
        var csv = Encoding.UTF8.GetString(bytes);
        Assert.Contains(code, csv);
        Assert.Contains("\"'=2+2\"", csv);
        Assert.Contains("1600000", csv);
        Assert.DoesNotContain(code, await admin.GetStringAsync("/Staff/Dashboard/Export?from=2030-05-01&to=2030-05-11"));
        using var receptionist = fixture.Client();
        await fixture.Login(receptionist, "staff");
        var denied = await receptionist.GetAsync("/Staff/Dashboard/Export");
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
        Assert.Contains("Denied", denied.Headers.Location?.ToString());
        var invalid = await admin.GetAsync("/Staff/Dashboard/Export?from=2030-06-12&to=2030-05-12");
        Assert.Equal(HttpStatusCode.Redirect, invalid.StatusCode);
    }

    [Fact]
    public async Task Dashboard_ready_room_count_excludes_dirty_inactive_and_reserved_rooms()
    {
        var initial = await fixture.InScope(async sp => await sp.GetRequiredService<ReportService>().DashboardAsync(fixture.Clock.Today, fixture.Clock.Today));
        await fixture.NewRoom(HousekeepingStatus.Dirty);
        await fixture.NewRoom(operation: OperationalStatus.OutOfOrder);
        var readyRoom = await fixture.NewRoom();
        var heldRoom = await fixture.NewRoom();
        var booking = await fixture.Reservations(service => service.Create(new BookingInput { RoomId = heldRoom, GuestProfileId = fixture.GuestId, GuestCount = 2, ArrivalDate = fixture.Clock.Today, DepartureDate = fixture.Clock.Today.AddDays(1) }, fixture.StaffId));
        await fixture.Reservations(service => service.Confirm(booking));
        var updated = await fixture.InScope(async sp => await sp.GetRequiredService<ReportService>().DashboardAsync(fixture.Clock.Today, fixture.Clock.Today));
        Assert.Equal(initial.Available + 1, updated.Available);
        Assert.Equal(initial.Dirty + 1, updated.Dirty);
    }

    [Fact]
    public async Task Password_change_requires_current_password_and_revokes_other_sessions()
    {
        var email = $"password-{Guid.NewGuid():N}@test.local";
        await fixture.InScope(async sp =>
        {
            var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = "Password test"
            };
            Assert.True((await users.CreateAsync(user, fixture.Password)).Succeeded);
            Assert.True((await users.AddToRoleAsync(user, Roles.Customer)).Succeeded);
            return true;
        });
        using var primary = fixture.Client();
        using var otherSession = fixture.Client();
        foreach (var client in new[]
        {
            primary,
            otherSession
        }

        )
        {
            var login = await fixture.Post(client, "/Account/Login", new() { ["Email"] = email, ["Password"] = fixture.Password });
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        }

        var newPassword = "New!" + Guid.NewGuid().ToString("N") + "A1";
        var data = new Dictionary<string, string>
        {
            ["CurrentPassword"] = "Incorrect!1",
            ["NewPassword"] = newPassword,
            ["ConfirmPassword"] = newPassword
        };
        var wrong = await fixture.Post(primary, "/Account/ChangePassword", data);
        Assert.Equal(HttpStatusCode.OK, wrong.StatusCode);
        Assert.Contains("Mật khẩu hiện tại chưa đúng", WebUtility.HtmlDecode(await wrong.Content.ReadAsStringAsync()));
        data["CurrentPassword"] = fixture.Password;
        var changed = await fixture.Post(primary, "/Account/ChangePassword", data);
        Assert.Equal(HttpStatusCode.Redirect, changed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await primary.GetAsync("/Account/ChangePassword")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await otherSession.GetAsync("/Account/ChangePassword")).StatusCode);
    }

    [Theory]
    [InlineData("arrivalDate=9999-12-31")]
    [InlineData("arrivalDate=invalid&guestCount=21")]
    public async Task Invalid_room_detail_dates_show_validation_instead_of_server_errors(string query)
    {
        var room = await fixture.NewRoom();
        using var client = fixture.Client();
        var response = await client.GetAsync($"/Rooms/Details/{room}?{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("validation-summary-errors", await response.Content.ReadAsStringAsync());
    }
}
