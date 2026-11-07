using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using AuroraHotel.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuroraHotel.Tests;
[Collection("Hotel SQL Server")]
public class BusinessTests(HotelFixture f)
{
    private BookingInput Input(int room, int start = 0, int end = 2) => new()
    {
        RoomId = room,
        GuestProfileId = f.GuestId,
        ArrivalDate = f.Clock.Today.AddDays(start),
        DepartureDate = f.Clock.Today.AddDays(end),
        GuestCount = 2
    };
    private Task<int> Book(int room, int start = 0, int end = 2) => f.Reservations(s => s.Create(Input(room, start, end), f.CustomerId, f.CustomerId));
    [Fact]
    public async Task Exact_demo_1800000_idempotent_payment_checkout_and_history()
    {
        var room = await f.NewRoom();
        var service = await f.NewService();
        var booking = await Book(room);
        await f.Reservations(s => s.Confirm(booking));
        await f.InScope(async sp =>
        {
            var db = sp.GetRequiredService<HotelDbContext>();
            (await db.Rooms.FindAsync(room))!.NightlyRate = 999000;
            await db.SaveChangesAsync();
            return true;
        });
        var folioId = await f.FrontDesk(s => s.CheckIn(booking));
        Assert.Equal(folioId, await f.FrontDesk(s => s.CheckIn(booking)));
        await f.FrontDesk(s => s.Charge(folioId, new ChargeInput { ServiceId = service, Quantity = 2 }));
        await f.InScope(async sp =>
        {
            var db = sp.GetRequiredService<HotelDbContext>();
            (await db.Services.FindAsync(service))!.UnitPrice = 777000;
            await db.SaveChangesAsync();
            return true;
        });
        var folio = await f.FrontDesk(s => s.ReadFolio(folioId));
        Assert.Equal(1600000, folio.Reservation.RoomTotal);
        Assert.Equal(1800000, folio.Total);
        Assert.Single(folio.Items, x => x.Kind == ItemKind.Room);
        await Assert.ThrowsAsync<BusinessException>(() => f.FrontDesk(s => s.CheckOut(folioId)));
        await Assert.ThrowsAsync<BusinessException>(() => f.FrontDesk(s => s.Pay(folioId, new PaymentInput { Amount = 1700000 }, f.StaffId)));
        await Assert.ThrowsAsync<BusinessException>(() => f.FrontDesk(s => s.Pay(folioId, new PaymentInput { Amount = 1900000 }, f.StaffId)));
        var payment = new PaymentInput
        {
            Amount = 1800000,
            Method = PaymentMethod.Cash
        };
        await Task.WhenAll(f.FrontDesk(s => s.Pay(folioId, payment, f.StaffId)), f.FrontDesk(s => s.Pay(folioId, payment, f.StaffId)));
        await Assert.ThrowsAsync<BusinessException>(() => f.FrontDesk(s => s.Charge(folioId, new ChargeInput { ServiceId = service, Quantity = 1 })));
        await f.FrontDesk(s => s.CheckOut(folioId));
        await f.FrontDesk(s => s.CheckOut(folioId));
        folio = await f.FrontDesk(s => s.ReadFolio(folioId));
        Assert.Equal(FolioStatus.Closed, folio.Status);
        Assert.Equal(ReservationStatus.CheckedOut, folio.Reservation.Status);
        Assert.Equal(0, folio.Balance);
        Assert.Equal(HousekeepingStatus.Dirty, folio.Reservation.Room.HousekeepingStatus);
        await f.InScope(async sp =>
        {
            var db = sp.GetRequiredService<HotelDbContext>();
            Assert.Equal(1, await db.Payments.CountAsync(x => x.FolioId == folioId));
            Assert.Equal(1800000, await db.Payments.Where(x => x.FolioId == folioId).SumAsync(x => x.Amount));
            return true;
        });
        await f.Rooms(s => s.SetHousekeeping(room, HousekeepingStatus.Clean));
        await f.FrontDesk(s => s.CheckOut(folioId)); // A duplicate checkout must not mark a cleaned room dirty again.
        Assert.Equal(HousekeepingStatus.Clean, (await f.FrontDesk(s => s.ReadFolio(folioId))).Reservation.Room.HousekeepingStatus);
        using var staff = f.Client();
        await f.Login(staff, "staff");
        foreach (var path in new[]
        {
            $"/Staff/Reservations/Details/{booking}",
            $"/Staff/FrontDesk/Folio/{folioId}",
            $"/Staff/FrontDesk/Invoice/{folioId}"}

        )
        {
            var response = await staff.GetAsync(path);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task Concurrent_confirmations_one_winner_across_two_contexts()
    {
        var room = await f.NewRoom();
        var first = await Book(room, 3, 5);
        var second = await Book(room, 3, 5);
        async Task<bool> Confirm(int id)
        {
            try
            {
                await f.Reservations(s => s.Confirm(id));
                return true;
            }
            catch (BusinessException)
            {
                return false;
            }
        }

        var outcomes = await Task.WhenAll(Confirm(first), Confirm(second));
        Assert.Single(outcomes, x => x);
        await f.InScope(async sp =>
        {
            var db = sp.GetRequiredService<HotelDbContext>();
            Assert.Equal(1, await db.Reservations.CountAsync(x => x.RoomId == room && x.Status == ReservationStatus.Confirmed));
            return true;
        });
    }

    [Fact]
    public async Task Pending_does_not_hold_adjacent_stays_allowed_dirty_not_excluded()
    {
        var room = await f.NewRoom(HousekeepingStatus.Dirty);
        var a = await Book(room, 5, 7);
        var b = await Book(room, 7, 9);
        var pendingOverlap = await Book(room, 6, 8);
        await f.Reservations(s => s.Confirm(a));
        await f.Reservations(s => s.Confirm(b));
        await Assert.ThrowsAsync<BusinessException>(() => f.Reservations(s => s.Confirm(pendingOverlap)));
        var type = await f.InScope(async sp => (await sp.GetRequiredService<HotelDbContext>().Rooms.FindAsync(room))!.RoomTypeId);
        var results = await f.Availability(s => s.Search(new SearchInput { ArrivalDate = f.Clock.Today.AddDays(10), DepartureDate = f.Clock.Today.AddDays(12), RoomTypeId = type }));
        Assert.Contains(results.Items, x => x.Id == room);
    }

    [Fact]
    public async Task Dates_capacity_out_of_order_and_changes_are_validated()
    {
        var room = await f.NewRoom();
        await Assert.ThrowsAsync<BusinessException>(() => Book(room, -1, 2));
        await Assert.ThrowsAsync<BusinessException>(() => Book(room, 2, 2));
        var input = Input(room);
        input.GuestCount = 3;
        await Assert.ThrowsAsync<BusinessException>(() => f.Reservations(s => s.Create(input, f.CustomerId, f.CustomerId)));
        var inactive = await f.NewRoom(operation: OperationalStatus.OutOfOrder);
        await Assert.ThrowsAsync<BusinessException>(() => Book(inactive));
        var a = await Book(room, 1, 3);
        await f.Reservations(s => s.Confirm(a));
        await Assert.ThrowsAsync<BusinessException>(() => f.Rooms(s => s.SetOperation(room, OperationalStatus.OutOfOrder)));
        var other = await f.NewRoom();
        var b = await Book(other, 1, 3);
        await f.Reservations(s => s.Confirm(b));
        await Assert.ThrowsAsync<BusinessException>(() => f.Reservations(s => s.Update(b, Input(room, 1, 3))));
    }

    [Fact]
    public async Task Checkin_requires_today_clean_active_and_vacant()
    {
        var room = await f.NewRoom(HousekeepingStatus.Dirty);
        var a = await Book(room);
        await f.Reservations(s => s.Confirm(a));
        await Assert.ThrowsAsync<BusinessException>(() => f.FrontDesk(s => s.CheckIn(a)));
        await f.Rooms(s => s.SetHousekeeping(room, HousekeepingStatus.Clean));
        var folio = await f.FrontDesk(s => s.CheckIn(a));
        await Assert.ThrowsAsync<BusinessException>(() => f.Reservations(s => s.Cancel(a)));
        await Assert.ThrowsAsync<BusinessException>(() => f.Rooms(s => s.SetOperation(room, OperationalStatus.OutOfOrder)));
        var future = await Book(await f.NewRoom(), 1, 3);
        await f.Reservations(s => s.Confirm(future));
        await Assert.ThrowsAsync<BusinessException>(() => f.FrontDesk(s => s.CheckIn(future)));
        Assert.Equal(folio, await f.FrontDesk(s => s.CheckIn(a)));
    }

    [Fact]
    public async Task Customer_cancellation_ownership_and_date_rules()
    {
        var a = await Book(await f.NewRoom(), 1, 3);
        await Assert.ThrowsAsync<BusinessException>(() => f.Reservations(s => s.Cancel(a, f.OtherId)));
        await f.Reservations(s => s.Cancel(a, f.CustomerId));
        await Assert.ThrowsAsync<BusinessException>(() => f.Reservations(s => s.Confirm(a)));
        var today = await Book(await f.NewRoom());
        await Assert.ThrowsAsync<BusinessException>(() => f.Reservations(s => s.Cancel(today, f.CustomerId)));
        var forged = Input(await f.NewRoom());
        forged.GuestProfileId = f.OtherGuestId;
        await Assert.ThrowsAsync<BusinessException>(() => f.Reservations(s => s.Create(forged, f.CustomerId, f.CustomerId)));
    }

    [Fact]
    public async Task Charge_edits_keep_price_remove_only_service_and_lock_after_payment()
    {
        var a = await Book(await f.NewRoom());
        await f.Reservations(s => s.Confirm(a));
        var folio = await f.FrontDesk(s => s.CheckIn(a));
        var service = await f.NewService();
        await f.FrontDesk(s => s.Charge(folio, new ChargeInput { ServiceId = service, Quantity = 1 }));
        var read = await f.FrontDesk(s => s.ReadFolio(folio));
        var charge = read.Items.Single(x => x.Kind == ItemKind.Service);
        await f.FrontDesk(s => s.UpdateCharge(folio, charge.Id, 2));
        Assert.Equal(1800000, (await f.FrontDesk(s => s.ReadFolio(folio))).Total);
        await Assert.ThrowsAsync<BusinessException>(() => f.FrontDesk(s => s.UpdateCharge(folio, charge.Id, 0)));
        await Assert.ThrowsAsync<BusinessException>(() => f.FrontDesk(s => s.UpdateCharge(folio, read.Items.Single(x => x.Kind == ItemKind.Room).Id, 0, true)));
        await f.FrontDesk(s => s.UpdateCharge(folio, charge.Id, 0, true));
        Assert.Equal(1600000, (await f.FrontDesk(s => s.ReadFolio(folio))).Total);
    }

    [Fact]
    public async Task Catalog_preserves_history_and_sanitizes_editor_content()
    {
        var room = await f.NewRoom();
        var a = await Book(room);
        await f.Reservations(s => s.Confirm(a));
        var folio = await f.FrontDesk(s => s.CheckIn(a));
        var service = await f.NewService();
        await f.FrontDesk(s => s.Charge(folio, new ChargeInput { ServiceId = service, Quantity = 1 }));
        await f.InScope(async sp =>
        {
            var catalog = sp.GetRequiredService<CatalogService>();
            var db = sp.GetRequiredService<HotelDbContext>();
            await Assert.ThrowsAsync<BusinessException>(() => catalog.DeleteRoom(room));
            var typeId = (await db.Rooms.AsNoTracking().SingleAsync(x => x.Id == room)).RoomTypeId;
            await Assert.ThrowsAsync<BusinessException>(() => catalog.DeleteType(typeId));
            await catalog.DeleteService(service);
            Assert.False((await db.Services.AsNoTracking().SingleAsync(x => x.Id == service)).IsActive);
            Assert.True(await db.FolioItems.AnyAsync(x => x.ServiceId == service));
            return true;
        });
        var sanitized = CatalogService.Sanitize("<p onclick='alert(1)'>Xin <strong>chào</strong></p><script>alert(1)</script><img src=x onerror=alert(1)>");
        Assert.DoesNotContain("script", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", sanitized);
        Assert.DoesNotContain("<img", sanitized);
        Assert.Contains("<strong>chào</strong>", sanitized);
    }
}
