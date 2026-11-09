namespace AuroraHotel.Services;

/// <summary>
/// Service xử lý tải lên hình ảnh cho phòng khách sạn.
/// Lưu trữ ảnh vào thư mục wwwroot/uploads của ứng dụng.
/// </summary>
public class RoomImageService
{
    private readonly IWebHostEnvironment _environment;

    public RoomImageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    // Hàm upload ảnh phòng
    public async Task<string?> Upload(IFormFile? file)
    {
        // Kiểm tra nếu không có file hoặc file rỗng thì bỏ qua
        if (file == null || file.Length == 0)
        {
            return null;
        }

        // Giới hạn kích thước file tối đa 5 MB
        if (file.Length > 5 * 1024 * 1024)
        {
            throw new BusinessException("Ảnh không được lớn hơn 5 MB.");
        }

        // Lấy phần mở rộng (đuôi file) và kiểm tra định dạng hợp lệ
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowedExtensions.Contains(extension))
        {
            throw new BusinessException("Chỉ nhận ảnh định dạng JPEG, PNG hoặc WebP.");
        }

        try
        {
            // Tạo tên file ngẫu nhiên bằng Guid để tránh trùng lặp tên file
            var fileName = Guid.NewGuid().ToString("N") + extension;

            // Đường dẫn thư mục lưu ảnh: wwwroot/uploads
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, fileName);

            // Lưu file vào thư mục
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Trả về đường dẫn tương đối để lưu vào database
            return "/uploads/" + fileName;
        }
        catch (Exception ex)
        {
            throw new BusinessException("Lỗi khi lưu ảnh: " + ex.Message);
        }
    }
}
