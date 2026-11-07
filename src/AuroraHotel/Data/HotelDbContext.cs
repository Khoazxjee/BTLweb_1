using AuroraHotel.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Data;

public class HotelDbContext(DbContextOptions<HotelDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<GuestProfile> GuestProfiles => Set<GuestProfile>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<HotelService> Services => Set<HotelService>();
    public DbSet<Folio> Folios => Set<Folio>();
    public DbSet<FolioItem> FolioItems => Set<FolioItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Room>().HasIndex(x => x.RoomNumber).IsUnique();
        b.Entity<GuestProfile>().HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
        b.Entity<Reservation>().HasIndex(x => x.ConfirmationCode).IsUnique();
        b.Entity<Reservation>().HasIndex(x => new { x.RoomId, x.Status, x.ArrivalDate, x.DepartureDate });
        b.Entity<Reservation>().HasIndex(x => x.RoomId).IsUnique().HasFilter("[Status] = 2");
        b.Entity<Folio>().HasOne(x => x.Reservation).WithOne(x => x.Folio).HasForeignKey<Folio>(x => x.ReservationId);
        b.Entity<Payment>().HasOne(x => x.Folio).WithOne(x => x.Payment).HasForeignKey<Payment>(x => x.FolioId);
        b.Entity<FolioItem>().HasIndex(x => x.FolioId).IsUnique().HasFilter("[Kind] = 0");
        b.Entity<Room>().ToTable(t => t.HasCheckConstraint("CK_Room_Rate", "[NightlyRate] > 0"));
        b.Entity<RoomType>().ToTable(t => t.HasCheckConstraint("CK_RoomType_Capacity", "[Capacity] BETWEEN 1 AND 20"));
        b.Entity<Reservation>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_Reservation_Dates", "[ArrivalDate] < [DepartureDate]");
            t.HasCheckConstraint("CK_Reservation_Values", "[GuestCount] > 0 AND [NightlyRateSnapshot] > 0");
        });
        b.Entity<FolioItem>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_FolioItem_Values", "[Quantity] > 0 AND [UnitPriceSnapshot] >= 0");
            t.HasCheckConstraint("CK_FolioItem_Kind", "([Kind] = 0 AND [ServiceId] IS NULL) OR ([Kind] = 1 AND [ServiceId] IS NOT NULL)");
        });
        b.Entity<HotelService>().ToTable(t => t.HasCheckConstraint("CK_Service_Price", "[UnitPrice] >= 0"));
        b.Entity<Payment>().ToTable(t => t.HasCheckConstraint("CK_Payment_Amount", "[Amount] > 0"));
        foreach (var entity in b.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(decimal)))
            {
                property.SetPrecision(18);
                property.SetScale(0);
            }

            foreach (var fk in entity.GetForeignKeys())
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
