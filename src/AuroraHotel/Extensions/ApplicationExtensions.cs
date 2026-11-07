using System.Globalization;
using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Extensions;

public static class ApplicationExtensions
{
    public static WebApplication UseHotelPipeline(this WebApplication app)
    {
        app.UseExceptionHandler("/Home/Error");
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.Use(async (context, next) =>
        {
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "DENY";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'; font-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
            await next();
        });
        app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");
        app.UseRequestLocalization(new RequestLocalizationOptions { DefaultRequestCulture = new RequestCulture("vi-VN"), SupportedCultures = [new CultureInfo("vi-VN")], SupportedUICultures = [new CultureInfo("vi-VN")] });
        app.UseStaticFiles();
        app.UseRouting();
        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
        app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        return app;
    }

    public static async Task InitializeHostingDemoAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var credentials = await scope.ServiceProvider.GetRequiredService<SeedData>().InitializeHostedDemo(app.Configuration["Bootstrap:DatabaseName"] ?? "", app.Configuration["Bootstrap:DemoPassword"] ?? "");
        var accountFolder = Path.Combine(app.Environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(accountFolder);
        await File.WriteAllLinesAsync(Path.Combine(accountFolder, "hosting-demo-accounts.txt"), new[] { "TÀI KHOẢN DEMO HOSTING — giữ riêng, không đưa lên GitHub. Mỗi tài khoản có mật khẩu riêng." }.Concat(credentials.Select(x => $"{x.Key} | {x.Value}")));
        Console.WriteLine("Đã khởi tạo database demo hosting: 4 tài khoản, 12 phòng và các đơn minh họa. Không khởi chạy web server.");
        Console.WriteLine("Thông tin đăng nhập riêng từng vai trò: App_Data/hosting-demo-accounts.txt trên máy chạy lệnh.");
    }

    public static async Task InitializeDevelopmentAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return;
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<HotelDbContext>().Database.MigrateAsync();
        if (app.Configuration.GetValue<bool>("SeedDemo"))
            await scope.ServiceProvider.GetRequiredService<SeedData>().Run();
    }
}
