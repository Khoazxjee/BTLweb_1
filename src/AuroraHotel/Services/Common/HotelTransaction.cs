using AuroraHotel.Data;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Services;

/// <summary>
/// Quản lý transaction cho các thao tác nghiệp vụ trong khách sạn.
/// Đảm bảo tính toàn vẹn dữ liệu khi thực hiện nhiều thao tác ghi.
/// </summary>
public class HotelTransaction
{
    private readonly HotelDbContext _db;

    public HotelTransaction(HotelDbContext db)
    {
        _db = db;
    }

    // Thực hiện hàm nghiệp vụ bất đồng bộ có trả về giá trị trong một transaction
    public async Task<T> RunAsync<T>(Func<Task<T>> action)
    {
        // Xóa bộ nhớ đệm theo dõi thực thể để tránh xung đột dữ liệu cũ
        _db.ChangeTracker.Clear();

        // Mở transaction mới
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Thực thi hành động nghiệp vụ
            var result = await action();

            // Lưu các thay đổi vào CSDL
            await _db.SaveChangesAsync();

            // Xác nhận hoàn thành transaction
            await tx.CommitAsync();

            return result;
        }
        catch
        {
            // Nếu có lỗi thì rollback giao dịch
            await tx.RollbackAsync();
            throw;
        }
    }

    // Thực hiện hàm nghiệp vụ không trả về giá trị
    public Task RunAsync(Func<Task> action) => RunAsync(async () =>
    {
        await action();
        return true;
    });
}
