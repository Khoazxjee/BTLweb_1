using System.Net;
using System.Security.Cryptography;
using AuroraHotel.Data;
using AuroraHotel.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuroraHotel.Tests;
public class HostingTests
{
    [Fact]
    public async Task Production_bootstrap_is_explicit_guarded_and_does_not_overwrite_accounts()
    {
        var database = "AuroraHotel_HostingTests_" + Guid.NewGuid().ToString("N");
        var password = "Demo!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12)) + "a1";
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:HotelDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database={database};Trusted_Connection=True;TrustServerCertificate=True" }));
        });
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
        try
        {
            // Constructing the Production host must not migrate or create any demo data.
            Assert.False(await db.Database.CanConnectAsync());
            var seed = scope.ServiceProvider.GetRequiredService<SeedData>();
            await seed.Run();
            Assert.False(await db.Database.CanConnectAsync());
            await Assert.ThrowsAsync<InvalidOperationException>(() => seed.InitializeHostedDemo("wrong-database", password));
            await Assert.ThrowsAsync<InvalidOperationException>(() => seed.InitializeHostedDemo(database, "weak"));
            Assert.False(await db.Database.CanConnectAsync());
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var accountFile = Path.Combine(env.ContentRootPath, "App_Data", "demo-accounts.txt");
            var previousWrite = File.GetLastWriteTimeUtc(accountFile);
            var credentials = await seed.InitializeHostedDemo(database, password);
            Assert.Equal(4, credentials.Values.Distinct().Count());
            Assert.Equal(4, await db.Users.CountAsync());
            Assert.Equal(12, await db.Rooms.CountAsync());
            Assert.Equal(5, await db.Reservations.CountAsync());
            Assert.Equal(previousWrite, File.GetLastWriteTimeUtc(accountFile));
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = (await users.FindByEmailAsync("admin@aurora.local"))!;
            Assert.True(await users.IsInRoleAsync(admin, Roles.Admin));
            Assert.True(await users.CheckPasswordAsync(admin, password));
            var staff = (await users.FindByEmailAsync("letan@aurora.local"))!;
            Assert.False(await users.CheckPasswordAsync(staff, password));
            Assert.True(await users.CheckPasswordAsync(staff, credentials[staff.Email!]));
            await Assert.ThrowsAsync<InvalidOperationException>(() => seed.InitializeHostedDemo(database, password + "Changed1!"));
            Assert.True(await users.CheckPasswordAsync(admin, password));
            Assert.Equal(4, await db.Users.CountAsync());
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
            var login = await client.GetAsync("/Account/Login");
            Assert.Contains(login.Headers.GetValues("Set-Cookie"), value => value.Contains("secure", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/initialize-demo")).StatusCode);
            var insecure = await client.GetAsync("http://localhost/");
            Assert.Equal(HttpStatusCode.TemporaryRedirect, insecure.StatusCode);
            Assert.Equal("https", insecure.Headers.Location!.Scheme);
        }
        finally
        {
            // This random name was created only for this test; never remove the user's database.
            if (!database.StartsWith("AuroraHotel_HostingTests_", StringComparison.Ordinal))
                throw new InvalidOperationException();
            await db.Database.EnsureDeletedAsync();
        }
    }
}
