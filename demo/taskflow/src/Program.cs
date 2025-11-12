using TaskFlow.Data;
using TaskFlow.Data.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Swap.Htmx;
using Swap.Htmx.Dev;
using TaskFlow.Events;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews(options =>
{
    // Accept both 123.45 and 123,45 for decimal inputs
    options.ModelBinderProviders.Insert(0, new TaskFlow.Infrastructure.InvariantDecimalModelBinderProvider());
});
// Swap + HTMX + Event System (central chains in Events/SwapEventChains.cs)
builder.Services.AddSwapHtmx(events =>
{
    SwapEventChains.Configure(events);
});

// Add session support for server-side bulk operations
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure Data Protection (for Docker containers)
var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
if (isDocker)
{
    var keysPath = "/app/keys";
    if (!Directory.Exists(keysPath))
    {
        Directory.CreateDirectory(keysPath);
    }
    
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("TaskFlow");
}

// Configure EF Core with sqlite

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));




var app = builder.Build();

// Auto-apply migrations in Development environment
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            // Log migration errors but don't crash on startup
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Error applying migrations. If using Docker volumes, try: docker compose down -v");
        }

        // Development seeding (controlled via env/config)
        try
        {
            var services = scope.ServiceProvider;
            var seedCountEnv = Environment.GetEnvironmentVariable("SEED_COUNT");
            var seedLocaleEnv = Environment.GetEnvironmentVariable("SEED_LOCALE");
            var seedIfEmptyEnv = Environment.GetEnvironmentVariable("SEED_IFEMPTY");

            var seedCount = int.TryParse(seedCountEnv, out var c) ? c : 50;
            var seedLocale = string.IsNullOrWhiteSpace(seedLocaleEnv) ? "en" : seedLocaleEnv;
            var seedIfEmpty = string.IsNullOrWhiteSpace(seedIfEmptyEnv) ? true :
                seedIfEmptyEnv.Equals("true", StringComparison.OrdinalIgnoreCase) || seedIfEmptyEnv == "1";

            await SeedRunner.RunAsync(db, services, seedCount, seedLocale, seedIfEmpty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Seeding error: {ex.Message}");
        }
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseSwapHtmxShell(); // Enforce HTMX partial responses
app.UseSwapHtmx(); // Event System middlewares (context + response)

app.UseSession(); // Enable session middleware

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Dev-only: event chains dashboard
if (app.Environment.IsDevelopment())
{
    app.MapSwapHtmxDevEndpoints();
}

app.Run();

// Expose Program for WebApplicationFactory (integration testing)
public partial class Program {}
