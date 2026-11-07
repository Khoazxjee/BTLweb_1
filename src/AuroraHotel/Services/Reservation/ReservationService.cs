using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Services;

public class ReservationService(HotelDbContext db, IHotelClock clock, HotelTransaction transaction, RoomAvailabilityService availability)
{
    public Task<int> Create(BookingInput input, string actor, string? owner = null) => transaction.RunAsync(async () =>
    {
        var guest = await db.GuestProfiles.FindAsync(input.GuestProfileId) ?? throw new BusinessException("Không tìm thấy hồ sơ khách.");
        if (owner != null && guest.UserId != owner)
            throw new BusinessException("Bạn không có quyền sử dụng hồ sơ này.");
        var room = await availability.AvailableRoom(input);
        var reservation = new Reservation
        {
            RoomId = room.Id,
            GuestProfileId = guest.Id,
            ConfirmationCode = "AU-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
            ArrivalDate = input.ArrivalDate,
            DepartureDate = input.DepartureDate,
            GuestCount = input.GuestCount,
            NightlyRateSnapshot = room.NightlyRate,
            Notes = input.Notes?.Trim() ?? "",
            CreatedAt = clock.Now,
            CreatedByUserId = actor,
            Source = owner == null ? ReservationSource.FrontDesk : ReservationSource.Website,
            Status = ReservationStatus.Pending
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();
        return reservation.Id;
    });
    public Task Update(int id, StayInput input) => transaction.RunAsync(async () =>
    {
        var reservation = await GetReservation(id);
        if (reservation.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
            throw new BusinessException("Chỉ sửa đơn chờ xác nhận hoặc đã xác nhận, trước khi nhận phòng.");
        var room = await availability.AvailableRoom(input, id);
        var changed = reservation.RoomId != input.RoomId || reservation.ArrivalDate != input.ArrivalDate || reservation.DepartureDate != input.DepartureDate;
        if (changed || reservation.Status == ReservationStatus.Pending)
            reservation.NightlyRateSnapshot = room.NightlyRate;
        reservation.RoomId = input.RoomId;
        reservation.ArrivalDate = input.ArrivalDate;
        reservation.DepartureDate = input.DepartureDate;
        reservation.GuestCount = input.GuestCount;
        reservation.Notes = input.Notes ?? "";
    });
    public Task Confirm(int id) => transaction.RunAsync(async () =>
    {
        var reservation = await GetReservation(id);
        if (reservation.Status == ReservationStatus.Confirmed)
            return;
        if (reservation.Status != ReservationStatus.Pending)
            throw new BusinessException("Chỉ xác nhận đơn đang chờ.");
        var room = await availability.AvailableRoom(new StayInput { RoomId = reservation.RoomId, ArrivalDate = reservation.ArrivalDate, DepartureDate = reservation.DepartureDate, GuestCount = reservation.GuestCount }, id);
        reservation.NightlyRateSnapshot = room.NightlyRate;
        reservation.Status = ReservationStatus.Confirmed;
    });
    public Task Cancel(int id, string? owner = null, bool reject = false) => transaction.RunAsync(async () =>
    {
        var reservation = await GetReservation(id);
        if (owner != null && reservation.GuestProfile.UserId != owner)
            throw new BusinessException("Không tìm thấy đơn đặt phòng của bạn.");
        if (reject && reservation.Status != ReservationStatus.Pending)
            throw new BusinessException("Chỉ từ chối đơn đang chờ.");
        if (reservation.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed))
            throw new BusinessException("Trạng thái này không cho phép hủy.");
        if (owner != null && reservation.ArrivalDate <= clock.Today)
            throw new BusinessException("Khách chỉ được hủy trước ngày nhận phòng. Vui lòng liên hệ lễ tân.");
        reservation.Status = reject ? ReservationStatus.Rejected : ReservationStatus.Cancelled;
    });
    private async Task<Reservation> GetReservation(int id) => await db.Reservations.Include(x => x.Room).Include(x => x.GuestProfile).SingleOrDefaultAsync(x => x.Id == id) ?? throw new BusinessException("Không tìm thấy đơn đặt phòng.");
}
