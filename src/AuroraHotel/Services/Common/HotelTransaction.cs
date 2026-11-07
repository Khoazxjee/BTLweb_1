using AuroraHotel.Data;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Services;

/// <summary>
/// Gom một thay đổi nghiệp vụ vào giao dịch SQL và khóa chung của khách sạn.
/// Khóa nằm ở database nên hai yêu cầu đồng thời không thể giữ cùng một phòng.
/// </summary>
public class HotelTransaction(HotelDbContext db)
{
    public async Task<T> RunAsync<T>(Func<Task<T>> action)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource=N'AuroraHotel:operations',
                @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
            IF @result < 0 THROW 51000, 'Hotel is busy; retry the operation.', 1;
            """);
        var result = await action();
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return result;
    }

    public Task RunAsync(Func<Task> action) => RunAsync(async () =>
    {
        await action();
        return true;
    });
}
