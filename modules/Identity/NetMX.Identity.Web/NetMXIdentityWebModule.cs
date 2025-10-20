using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NetMX.Core;
using NetMX.Identity.Core.Data;
using NetMX.Identity.Core.Users;

namespace NetMX.Identity.Web;

/// <summary>
/// Module for configuring ASP.NET Core Identity in the NetMX Identity Web layer.
/// </summary>
public class NetMXIdentityWebModule : NetMXModule
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Configure ASP.NET Core Identity
        services.AddIdentity<AppUser, AppRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;

            // Sign in settings
            options.SignIn.RequireConfirmedEmail = false; // Can be enabled later
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

        // Configure application cookie
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(1);
            options.SlidingExpiration = true;
            options.LoginPath = "/account/login";
            options.LogoutPath = "/account/logout";
            options.AccessDeniedPath = "/account/access-denied";
        });
    }
}
