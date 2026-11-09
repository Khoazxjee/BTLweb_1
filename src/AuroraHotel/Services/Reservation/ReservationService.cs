using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Services;

/// <summary>
/// Service quản lý vòng đời đơn đặt phòng (Tạo, sửa lịch, duyệt/xác nhận, hủy đơn).
/// </summary>
public class ReservationService
{
    private readonly HotelDbContext _db;
    private readonly IHotelClock _clock;
    private readonly HotelTransaction _transaction;
    private readonly RoomAvailabilityService _availability;

    public ReservationService(HotelDbContext db, IHotelClock clock, HotelTransaction transaction, RoomAvailabilityService availability)
    {
        _db = db;
        _clock = clock;
        _transaction = transaction;
        _availability = availability;
    }

    // Tạo đơn đặt phòng mới (Khách tự đặt hoặc Lễ tân tạo tại quầy)
    public Task<int> Create(BookingInput input, string actor, string? owner = null) => _transaction.RunAsync(async () =>
    {
        // 1. Kiểm tra hồ sơ khách hàng
        var guest = await _db.GuestProfiles.FindAsync(input.GuestProfileId)
            ?? throw new BusinessException("Không tìm thấy hồ sơ khách.");

        if (owner != null && guest.UserId != owner)
            throw new BusinessException("Bạn không có quyền sử dụng hồ sơ này.");

        // 2. Kiểm tra xem phòng còn trống trong khoảng ngày này không
        var room = await _availability.AvailableRoom(input);

        // 3. Khởi tạo đơn đặt phòng mới với trạng thái Chờ duyệt (Pending)
        var reservation = new Reservation
        {
            RoomId = room.Id,
            GuestProfileId = guest.Id,
            ConfirmationCode = "AU-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
            ArrivalDate = input.ArrivalDate,
            DepartureDate = input.DepartureDate,
            GuestCount = input.GuestCount,
            NightlyRateSnapshot = room.NightlyRate, // Chốt giá tại thời điểm đặt
            Notes = input.Notes?.Trim() ?? "",
            CreatedAt = _clock.Now,
            CreatedByUserId = actor,
            Source = owner == null ? ReservationSource.FrontDesk : ReservationSource.Website,
            Status = ReservationStatus.Pending
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();
        return reservation.Id;
    });

    // Cập nhật lịch hoặc đổi phòng trước khi nhận phòng
    public Task Update(int id, StayInput input) => _transaction.RunAsync(async () =>
    {
        var reservation = await GetReservation(id);

        if (reservation.Status != ReservationStatus.Pending && reservation.Status != ReservationStatus.Confirmed)
            throw new BusinessException("Chỉ sửa đơn chờ xác nhận hoặc đã xác nhận, trước khi nhận phòng.");

        // Kiểm tra phòng mới có khả dụng trong khoảng thời gian mới không
        var room = await _availability.AvailableRoom(input, id);

        var changed = reservation.RoomId != input.RoomId || reservation.ArrivalDate != input.ArrivalDate || reservation.DepartureDate != input.DepartureDate;
        if (changed || reservation.Status == ReservationStatus.Pending)
            reservation.NightlyRateSnapshot = room.NightlyRate;

        // Cập nhật thông tin mới
        reservation.RoomId = input.RoomId;
        reservation.ArrivalDate = input.ArrivalDate;
        reservation.DepartureDate = input.DepartureDate;
        reservation.GuestCount = input.GuestCount;
        reservation.Notes = input.Notes ?? "";
    });

    // Duyệt / Xác nhận đơn đặt phòng (chỉ Lễ tân hoặc Admin thực hiện)
    public Task Confirm(int id) => _transaction.RunAsync(async () =>
    {
        var reservation = await GetReservation(id);

        if (reservation.Status == ReservationStatus.Confirmed)
            return;

        if (reservation.Status != ReservationStatus.Pending)
            throw new BusinessException("Chỉ xác nhận đơn đang chờ.");

        // Kiểm tra phòng trống lần cuối trước khi chốt
        var room = await _availability.AvailableRoom(new StayInput
        {
            RoomId = reservation.RoomId,
            ArrivalDate = reservation.ArrivalDate,
            DepartureDate = reservation.DepartureDate,
            GuestCount = reservation.GuestCount
        }, id);

        reservation.NightlyRateSnapshot = room.NightlyRate;
        reservation.Status = ReservationStatus.Confirmed;
    });

    // Hủy đơn hoặc từ chối đơn đặt phòng
    public Task Cancel(int id, string? owner = null, bool reject = false) => _transaction.RunAsync(async () =>
    {
        var reservation = await GetReservation(id);

        // Kiểm tra quyền sở hữu đơn nếu là khách hàng tự hủy
        if (owner != null && reservation.GuestProfile.UserId != owner)
            throw new BusinessException("Không tìm thấy đơn đặt phòng của bạn.");

        if (reject && reservation.Status != ReservationStatus.Pending)
            throw new BusinessException("Chỉ từ chối đơn đang chờ.");

        if (reservation.Status != ReservationStatus.Pending && reservation.Status != ReservationStatus.Confirmed)
            throw new BusinessException("Trạng thái này không cho phép hủy.");

        // Khách hàng chỉ được hủy trước ngày đến
        if (owner != null && reservation.ArrivalDate <= _clock.Today)
            throw new BusinessException("Khách chỉ được hủy trước ngày nhận phòng. Vui lòng liên hệ lễ tân.");

        reservation.Status = reject ? ReservationStatus.Rejected : ReservationStatus.Cancelled;
    });

    // Lấy thông tin đơn đặt phòng kèm chi tiết phòng và khách
    private async Task<Reservation> GetReservation(int id) =>
        await _db.Reservations.Include(x => x.Room).Include(x => x.GuestProfile).SingleOrDefaultAsync(x => x.Id == id)
        ?? throw new BusinessException("Không tìm thấy đơn đặt phòng.");
}
