using AuroraHotel.Extensions;

var initializeDemo = args.Contains("--initialize-demo", StringComparer.Ordinal);

// 1. Tự động xác định thư mục chứa mã nguồn và file tĩnh (wwwroot)
var contentRoot = Directory.GetCurrentDirectory();
if (!Directory.Exists(Path.Combine(contentRoot, "wwwroot")))
{
    // Nếu chạy từ thư mục gốc solution (D:\ANGULAR\BTLweb_1-main)
    if (Directory.Exists(Path.Combine(contentRoot, "src", "AuroraHotel", "wwwroot")))
    {
        contentRoot = Path.Combine(contentRoot, "src", "AuroraHotel");
    }
    // Nếu chạy trực tiếp file .exe từ thư mục bin/Debug/net10.0
    else
    {
        var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        if (Directory.Exists(Path.Combine(projectDir, "wwwroot")))
        {
            contentRoot = projectDir;
        }
    }
}

// 2. Khởi tạo WebApplicationOptions với thư mục gốc và môi trường chuẩn
var options = new WebApplicationOptions
{
    Args = args.Where(x => x != "--initialize-demo").ToArray(),
    ContentRootPath = contentRoot,
    WebRootPath = Path.Combine(contentRoot, "wwwroot"),
    EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environments.Development
};

var builder = WebApplication.CreateBuilder(options);

// Tải thêm cấu hình từ file appsettings
builder.Configuration
    .SetBasePath(contentRoot)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddHotelServices(builder.Configuration, builder.Environment);

var app = builder.Build();

if (initializeDemo)
{
    await app.InitializeHostingDemoAsync();
    return;
}

app.UseHotelPipeline();
await app.InitializeDevelopmentAsync();
app.Run();

public partial class Program;
