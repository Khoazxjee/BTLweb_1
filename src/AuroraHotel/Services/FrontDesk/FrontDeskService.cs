using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Services;

/// <summary>
/// Service phụ trách các nghiệp vụ Lễ tân tại quầy:
/// Nhận phòng (Check-in), gọi dịch vụ phòng, ghi nhận thanh toán và trả phòng (Check-out).
/// </summary>
public class FrontDeskService
{
    private readonly HotelDbContext _db;
    private readonly IHotelClock _clock;
    private readonly HotelTransaction _transaction;

    public FrontDeskService(HotelDbContext db, IHotelClock clock, HotelTransaction transaction)
    {
        _db = db;
        _clock = clock;
        _transaction = transaction;
    }

    // Nghiệp vụ Nhận phòng (Check-in)
    public Task<int> CheckIn(int id) => _transaction.RunAsync(async () =>
    {
        // 1. Tìm đơn đặt phòng
        var reservation = await GetReservation(id);

        // Nếu đã nhận phòng rồi thì trả về mã bảng kê hiện tại
        if (reservation.Status == ReservationStatus.CheckedIn)
            return (await _db.Folios.SingleAsync(x => x.ReservationId == id)).Id;

        // 2. Kiểm tra điều kiện nhận phòng
        if (reservation.Status != ReservationStatus.Confirmed)
            throw new BusinessException("Đơn phải được xác nhận trước khi nhận phòng.");

        if (reservation.ArrivalDate != _clock.Today)
            throw new BusinessException("Chỉ nhận phòng đúng ngày đã chốt. Hãy điều chỉnh lịch hợp lệ trước.");

        var room = reservation.Room;
        if (room.OperationalStatus != OperationalStatus.Active || room.HousekeepingStatus != HousekeepingStatus.Clean)
            throw new BusinessException("Phòng cần đang hoạt động và đã dọn sạch để nhận khách.");

        if (await _db.Reservations.AnyAsync(x => x.RoomId == room.Id && x.Status == ReservationStatus.CheckedIn))
            throw new BusinessException("Phòng vẫn còn khách đang lưu trú.");

        // 3. Cập nhật trạng thái đơn thành CheckedIn
        reservation.Status = ReservationStatus.CheckedIn;
        reservation.CheckInAt = _clock.Now;

        // 4. Khởi tạo bảng kê (Folio) và thêm phí phòng
        var folio = new Folio
        {
            ReservationId = id,
            CreatedAt = _clock.Now,
            Items = new List<FolioItem>
            {
                new FolioItem
                {
                    Kind = ItemKind.Room,
                    DescriptionSnapshot = $"Phòng {room.RoomNumber} · {Ui.Date(reservation.ArrivalDate)} – {Ui.Date(reservation.DepartureDate)}",
                    Quantity = reservation.Nights,
                    UnitPriceSnapshot = reservation.NightlyRateSnapshot
                }
            }
        };

        _db.Folios.Add(folio);
        await _db.SaveChangesAsync();
        return folio.Id;
    });

    // Lấy thông tin đơn đặt phòng kèm phòng và hồ sơ khách
    private async Task<Reservation> GetReservation(int id) =>
        await _db.Reservations.Include(x => x.Room).Include(x => x.GuestProfile).SingleOrDefaultAsync(x => x.Id == id)
        ?? throw new BusinessException("Không tìm thấy đơn đặt phòng.");

    // Đọc thông tin bảng kê cho trang hiển thị hoặc in hóa đơn
    public async Task<Folio> ReadFolio(int id) =>
        await _db.Folios.AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.Payment!).ThenInclude(x => x.ReceivedByUser)
            .Include(x => x.Reservation).ThenInclude(x => x.GuestProfile)
            .Include(x => x.Reservation).ThenInclude(x => x.Room)
            .SingleOrDefaultAsync(x => x.Id == id)
        ?? throw new BusinessException("Không tìm thấy bảng kê.");

    // Lấy bảng kê để chỉnh sửa (có tracking)
    private async Task<Folio> EditFolio(int id) =>
        await _db.Folios
            .Include(x => x.Items)
            .Include(x => x.Payment)
            .Include(x => x.Reservation).ThenInclude(x => x.Room)
            .SingleOrDefaultAsync(x => x.Id == id)
        ?? throw new BusinessException("Không tìm thấy bảng kê.");

    // Kiểm tra bảng kê có đang được phép chỉnh sửa dịch vụ không
    private static void EnsureEditable(Folio folio)
    {
        if (folio.Status != FolioStatus.Open || folio.Payment != null || folio.Reservation.Status != ReservationStatus.CheckedIn)
            throw new BusinessException("Chỉ sửa dịch vụ khi khách đang lưu trú và chưa thanh toán.");
    }

    // Thêm phí dịch vụ phòng vào bảng kê
    public Task Charge(int folioId, ChargeInput input) => _transaction.RunAsync(async () =>
    {
        var folio = await EditFolio(folioId);
        EnsureEditable(folio);

        if (input.Quantity < 1 || input.Quantity > 1000)
            throw new BusinessException("Số lượng phải từ 1 đến 1.000.");

        var service = await _db.Services.SingleOrDefaultAsync(x => x.Id == input.ServiceId && x.IsActive)
            ?? throw new BusinessException("Dịch vụ không còn cung cấp.");

        folio.Items.Add(new FolioItem
        {
            Kind = ItemKind.Service,
            ServiceId = service.Id,
            DescriptionSnapshot = service.Name + " / " + service.Unit,
            Quantity = input.Quantity,
            UnitPriceSnapshot = service.UnitPrice
        });
    });

    // Cập nhật số lượng hoặc xóa dịch vụ khỏi bảng kê
    public Task UpdateCharge(int folioId, int itemId, int quantity, bool delete = false) => _transaction.RunAsync(async () =>
    {
        var folio = await EditFolio(folioId);
        EnsureEditable(folio);

        var item = folio.Items.SingleOrDefault(x => x.Id == itemId && x.Kind == ItemKind.Service)
            ?? throw new BusinessException("Không tìm thấy khoản dịch vụ.");

        if (delete)
        {
            _db.FolioItems.Remove(item);
        }
        else
        {
            if (quantity < 1 || quantity > 1000)
                throw new BusinessException("Số lượng phải từ 1 đến 1.000.");
            item.Quantity = quantity;
        }
    });

    // Kiểm tra tiền phòng trong bảng kê trước khi thanh toán
    private static void ValidateRoomCharge(Folio folio)
    {
        var roomLines = folio.Items.Where(x => x.Kind == ItemKind.Room).ToList();
        if (roomLines.Count != 1 || roomLines[0].Quantity != folio.Reservation.Nights || roomLines[0].UnitPriceSnapshot != folio.Reservation.NightlyRateSnapshot || folio.Total <= 0)
            throw new BusinessException("Bảng kê chưa có đúng phí phòng. Không thể thanh toán hoặc trả phòng.");
    }

    // Ghi nhận thanh toán tại quầy
    public Task Pay(int folioId, PaymentInput input, string actor) => _transaction.RunAsync(async () =>
    {
        if (!Enum.IsDefined(input.Method))
            throw new BusinessException("Phương thức thanh toán không hợp lệ.");

        var folio = await EditFolio(folioId);

        // Nếu đã thanh toán cùng số tiền thì bỏ qua
        if (folio.Payment != null)
        {
            if (folio.Payment.Amount == input.Amount && folio.Payment.Method == input.Method)
                return;
            throw new BusinessException("Bảng kê đã được thanh toán. Không thể thu thêm hoặc thay đổi thanh toán.");
        }

        EnsureEditable(folio);
        ValidateRoomCharge(folio);

        // Kiểm tra số tiền khách đưa phải bằng tổng tiền trên bảng kê
        if (input.Amount <= 0 || input.Amount != folio.Total)
            throw new BusinessException("Số tiền nhận phải bằng chính xác tổng phải thu.");

        // Tạo bản ghi thanh toán
        folio.Payment = new Payment
        {
            Amount = folio.Total,
            Method = input.Method,
            PaidAt = _clock.Now,
            ReceivedByUserId = actor
        };

        folio.Status = FolioStatus.Paid;
    });

    // Nghiệp vụ Trả phòng (Check-out)
    public Task CheckOut(int folioId) => _transaction.RunAsync(async () =>
    {
        var folio = await EditFolio(folioId);

        if (folio.Status == FolioStatus.Closed && folio.Reservation.Status == ReservationStatus.CheckedOut)
            return;

        ValidateRoomCharge(folio);

        // Bắt buộc phải thanh toán đủ trước khi trả phòng
        if (folio.Reservation.Status != ReservationStatus.CheckedIn || folio.Status != FolioStatus.Paid || folio.Payment == null || folio.Balance != 0)
            throw new BusinessException("Chỉ trả phòng khi khách đang ở và đã thanh toán đủ.");

        // Cập nhật trạng thái trả phòng
        folio.Reservation.Status = ReservationStatus.CheckedOut;
        folio.Reservation.CheckOutAt = _clock.Now;
        folio.Status = FolioStatus.Closed;
        folio.ClosedAt = _clock.Now;

        // Chuyển phòng sang trạng thái Cần dọn (Dirty)
        folio.Reservation.Room.HousekeepingStatus = HousekeepingStatus.Dirty;
    });
}
