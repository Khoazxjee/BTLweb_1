using AuroraHotel.Data;
using AuroraHotel.Models;
using AuroraHotel.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuroraHotel.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHotelServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.Configure<HotelOptions>(configuration.GetSection("Hotel"));
        services.AddDbContext<HotelDbContext>(o => o.UseSqlServer(configuration.GetConnectionString("HotelDb") ?? throw new InvalidOperationException("Configure ConnectionStrings:HotelDb before starting.")));
        services.AddIdentity<ApplicationUser, IdentityRole>(o =>
        {
            o.User.RequireUniqueEmail = true;
            o.Password.RequiredLength = 8;
            o.Lockout.MaxFailedAccessAttempts = 5;
            o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        }).AddEntityFrameworkStores<HotelDbContext>().AddDefaultTokenProviders();
        services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);
        services.ConfigureApplicationCookie(o =>
        {
            o.LoginPath = "/Account/Login";
            o.AccessDeniedPath = "/Account/Denied";
            o.Cookie.Name = "Aurora.Auth";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;
            o.Cookie.SecurePolicy = environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            o.ExpireTimeSpan = TimeSpan.FromHours(8);
            o.Events.OnRedirectToLogin = c =>
            {
                if (c.Request.Path.StartsWithSegments("/api"))
                    c.Response.StatusCode = 401;
                else
                    c.Response.Redirect(c.RedirectUri);
                return Task.CompletedTask;
            };
            o.Events.OnRedirectToAccessDenied = c =>
            {
                if (c.Request.Path.StartsWithSegments("/api"))
                    c.Response.StatusCode = 403;
                else
                    c.Response.Redirect(c.RedirectUri);
                return Task.CompletedTask;
            };
        });
        services.AddControllersWithViews(o =>
        {
            o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            o.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "Vui lòng nhập giá trị.");
            o.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((_, name) => $"Giá trị {name} không hợp lệ.");
        });
        services.AddAntiforgery(o =>
        {
            o.HeaderName = "X-CSRF-TOKEN";
            o.Cookie.SecurePolicy = environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });
        services.AddDistributedMemoryCache();
        services.AddSession(o =>
        {
            o.Cookie.Name = "Aurora.Selection";
            o.Cookie.HttpOnly = true;
            o.Cookie.IsEssential = true;
            o.Cookie.SecurePolicy = environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            o.IdleTimeout = TimeSpan.FromMinutes(30);
        });
        services.AddHttpsRedirection(o => o.HttpsPort = 443);
        services.AddSingleton<IHotelClock, HotelClock>();
        services.AddScoped<HotelTransaction>();
        services.AddScoped<RoomAvailabilityService>();
        services.AddScoped<ReservationService>();
        services.AddScoped<FrontDeskService>();
        services.AddScoped<RoomStatusService>();
        services.AddScoped<RoomImageService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<SeedData>();
        services.AddScoped<ReportService>();
        return services;
    }
}
