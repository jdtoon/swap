using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using SwapSmallPartials.Data;
using Swap.Htmx;

namespace SwapSmallPartials.Infrastructure;

public static class ApplicationBuilderExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Use EnsureCreated for initial development (no migrations needed)
        // Switch to MigrateAsync() once you add migrations to your project
        await db.Database.EnsureCreatedAsync();

        // Enable WAL mode for better concurrent access (SQLite)
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseResponseCompression();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseWebOptimizer();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseSwapHtmx();
        app.UseAuthorization();

        return app;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description
                    })
                };
                await context.Response.WriteAsJsonAsync(result);
            }
        });

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        return app;
    }
}