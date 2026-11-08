using System.Net;
using System.Text.RegularExpressions;
using AuroraHotel.Data;
using AuroraHotel.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuroraHotel.Tests;

[Collection("Hotel SQL Server")]
public class AjaxEditorTests(HotelFixture fixture)
{
    [Fact]
    public async Task Ajax_search_paginates_filtered_results_and_keeps_a_full_page_fallback()
    {
        var firstRoomId = await fixture.NewRoom();
        var typeId = await fixture.InScope(async services =>
        {
            var db = services.GetRequiredService<HotelDbContext>();
            var typeId = await db.Rooms.Where(room => room.Id == firstRoomId).Select(room => room.RoomTypeId).SingleAsync();
            for (var i = 0; i < 9; i++)
                db.Rooms.Add(new Room { RoomNumber = "AJ" + Guid.NewGuid().ToString("N")[..12], RoomTypeId = typeId, NightlyRate = 800000 });
            await db.SaveChangesAsync();
            return typeId;
        });
        var query = $"/Rooms?ArrivalDate=2030-06-01&DepartureDate=2030-06-03&RoomTypeId={typeId}&MaxPrice=900000";
        using var client = fixture.Client();
        var fullPage = await client.GetStringAsync(query);
        Assert.Contains("<!DOCTYPE html>", fullPage);
        Assert.Contains("method=\"get\"", fullPage);
        Assert.Contains("/js/room-search.js", fullPage);

        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        var response = await client.GetAsync(query + "&Page=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("X-Requested-With", response.Headers.Vary);
        var firstPage = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain("<!DOCTYPE", firstPage);
        Assert.DoesNotContain("<script", firstPage);
        Assert.Contains("data-total=\"10\"", firstPage);
        Assert.Equal(9, Regex.Matches(firstPage, "<article class=\"room-card\"").Count);
        var pageLink = Regex.Match(firstPage, "href=\"([^\"]+)\" class=\"page-link \"[^>]*>2</a>");
        Assert.True(pageLink.Success);
        Assert.Contains($"RoomTypeId={typeId}", pageLink.Groups[1].Value);
        Assert.Contains("MaxPrice=900000", pageLink.Groups[1].Value);
        Assert.Single(Regex.Matches(pageLink.Groups[1].Value, "[?&]page=", RegexOptions.IgnoreCase));
        var secondPage = await client.GetStringAsync(pageLink.Groups[1].Value);
        Assert.Single(Regex.Matches(secondPage, "<article class=\"room-card\""));
    }

    [Fact]
    public async Task Ajax_search_returns_validation_and_empty_states_without_a_server_error()
    {
        using var client = fixture.Client();
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        var invalid = await client.GetAsync("/Rooms?ArrivalDate=2030-06-03&DepartureDate=2030-06-01");
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        Assert.Contains("validation-summary-errors", await invalid.Content.ReadAsStringAsync());
        var empty = await client.GetStringAsync("/Rooms?ArrivalDate=2030-06-01&DepartureDate=2030-06-03&RoomTypeId=2147483647");
        Assert.Contains("data-total=\"0\"", empty);
        Assert.Contains("empty-state", empty);
    }

    [Fact]
    public async Task Editor_form_persists_safe_formatting_and_preserves_validation_and_csrf()
    {
        var existingId = await fixture.NewRoom();
        var typeId = await fixture.InScope(services => services.GetRequiredService<HotelDbContext>().Rooms
            .Where(room => room.Id == existingId).Select(room => room.RoomTypeId).SingleAsync());
        using var admin = fixture.Client();
        await fixture.Login(admin, "admin");
        var form = await admin.GetStringAsync("/Staff/Catalog/Room");
        Assert.Contains("data-rich-editor", form);
        Assert.Contains("<textarea", form);
        Assert.Contains("/lib/quill/quill.js", form);
        var number = "ED" + Guid.NewGuid().ToString("N")[..12];
        var values = new Dictionary<string, string>
        {
            ["RoomNumber"] = number,
            ["RoomTypeId"] = typeId.ToString(),
            ["NightlyRate"] = "800000",
            ["Description"] = "<h3>Phòng đẹp</h3><p onclick='attack()'><strong>Đậm</strong> <em>Nghiêng</em></p><ul><li>Wi-Fi</li></ul><ol><li>Bữa sáng</li></ol><script>attack()</script><img src=x onerror=attack()>"
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("/Staff/Catalog/Room", new FormUrlEncodedContent(values))).StatusCode);
        var saved = await fixture.Post(admin, "/Staff/Catalog/Room", values, "/Staff/Catalog/Room");
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        var room = await fixture.InScope(services => services.GetRequiredService<HotelDbContext>().Rooms.AsNoTracking()
            .SingleAsync(item => item.RoomNumber == number));
        foreach (var tag in new[] { "<h3>", "<strong>", "<em>", "<ul>", "<ol>", "<li>" })
            Assert.Contains(tag, room.Description);
        foreach (var forbidden in new[] { "<script", "onclick", "<img", "onerror" })
            Assert.DoesNotContain(forbidden, room.Description, StringComparison.OrdinalIgnoreCase);
        var publicPage = WebUtility.HtmlDecode(await fixture.Client().GetStringAsync($"/Rooms/Details/{room.Id}"));
        Assert.Contains("<strong>Đậm</strong>", publicPage);
        var editPage = WebUtility.HtmlDecode(await admin.GetStringAsync($"/Staff/Catalog/Room/{room.Id}"));
        Assert.Contains("<strong>Đậm</strong>", editPage);

        values["Id"] = room.Id.ToString();
        values["NightlyRate"] = "0";
        values["Description"] = "<p><strong>Nội dung chưa lưu</strong></p>";
        var invalid = await fixture.Post(admin, "/Staff/Catalog/Room", values, "/Staff/Catalog/Room");
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        var invalidPage = WebUtility.HtmlDecode(await invalid.Content.ReadAsStringAsync());
        Assert.Contains("validation-summary-errors", invalidPage);
        Assert.Contains(values["Description"], invalidPage);
        Assert.Equal(room.Description, await fixture.InScope(services => services.GetRequiredService<HotelDbContext>().Rooms
            .Where(item => item.Id == room.Id).Select(item => item.Description).SingleAsync()));
    }
}
