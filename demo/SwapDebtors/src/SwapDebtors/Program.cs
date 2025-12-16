using Microsoft.EntityFrameworkCore;
using Swap.Htmx;
using SwapDebtors.Data;
using SwapDebtors.Events;
using SwapDebtors.Services;

var builder = WebApplication.CreateBuilder(args);

// Add controllers with views
builder.Services.AddControllersWithViews();

// Add SQLite with EF Core
builder.Services.AddDbContext<DebtorsDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
                      ?? "Data Source=debtors.db"));

// Add HttpClient for currency API
builder.Services.AddHttpClient<ICurrencyService, CurrencyService>();

// Add session for potential future use
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add Swap.Htmx with event configurations
builder.Services.AddSwapHtmx(options =>
{
    // Auto-suppress layout for HTMX requests
    options.AutoSuppressLayout = true;
    
    // Default navigation target for <swap-nav>
    options.DefaultNavigationTarget = "#main-content";
    
    // Configure view search paths for cross-controller OOB swaps
    options.PartialViewSearchPaths.Add("Dashboard");
    options.PartialViewSearchPaths.Add("Debtors");
    options.PartialViewSearchPaths.Add("Debts");
    
    // Register event configurations
    options.AddConfig<DebtorEventConfig>();
    options.AddConfig<DebtEventConfig>();
});

var app = builder.Build();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DebtorsDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Dashboard/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Swap.Htmx middleware
app.UseSwapHtmx();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// SSE endpoint for activity stream
app.MapGet("/api/activity-stream", async (HttpContext context, CancellationToken ct) =>
{
    context.Response.Headers.Append("Content-Type", "text/event-stream");
    context.Response.Headers.Append("Cache-Control", "no-cache");
    context.Response.Headers.Append("Connection", "keep-alive");
    
    // Keep connection open and send heartbeats
    while (!ct.IsCancellationRequested)
    {
        await context.Response.WriteAsync($": heartbeat\n\n", ct);
        await context.Response.Body.FlushAsync(ct);
        await Task.Delay(30000, ct); // 30 second heartbeat
    }
});

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
