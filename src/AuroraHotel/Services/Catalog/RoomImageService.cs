using SkiaSharp;

namespace AuroraHotel.Services;

public class RoomImageService(IWebHostEnvironment environment)
{
    public async Task<string?> Upload(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return null;
        if (file.Length > 5 * 1024 * 1024)
            throw new BusinessException("Ảnh không được lớn hơn 5 MB.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        }.Contains(extension))
            throw new BusinessException("Chỉ nhận ảnh JPEG, PNG hoặc WebP.");
        try
        {
            await using var stream = file.OpenReadStream();
            using var codec = SKCodec.Create(stream);
            if (codec == null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
                throw new BusinessException("Nội dung tệp không phải JPEG, PNG hoặc WebP hợp lệ.");
            var info = codec.Info;
            if (info.Width > 8000 || info.Height > 8000 || (long)info.Width * info.Height > 32000000)
                throw new BusinessException("Ảnh quá lớn; tối đa 8.000 px mỗi chiều và 32 megapixel.");
            using var bitmap = SKBitmap.Decode(codec);
            if (bitmap == null)
                throw new BusinessException("Tệp ảnh bị hỏng.");
            using var image = SKImage.FromBitmap(bitmap);
            var name = Guid.NewGuid().ToString("N") + ".webp";
            var folder = Path.Combine(environment.WebRootPath, "uploads");
            Directory.CreateDirectory(folder);
            // Decode then re-encode: user content and filenames never become executable files.
            using var data = image.Encode(SKEncodedImageFormat.Webp, 85);
            await File.WriteAllBytesAsync(Path.Combine(folder, name), data.ToArray());
            return "/uploads/" + name;
        }
        catch (ArgumentException)
        {
            throw new BusinessException("Không đọc được ảnh. Vui lòng chọn tệp ảnh hợp lệ.");
        }
    }
}
