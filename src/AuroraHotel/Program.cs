using AuroraHotel.Extensions;

var initializeDemo = args.Contains("--initialize-demo", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(x => x != "--initialize-demo").ToArray());
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true).AddEnvironmentVariables();
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
