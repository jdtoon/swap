using Swap.Htmx;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSwapHtmx(options =>
{
    // Auto-suppress layout for HTMX requests - no _ViewStart.cshtml needed!
    options.AutoSuppressLayout = true;
    
    // Default navigation target for <swap-nav> tag helper
    options.DefaultNavigationTarget = "#main-content";
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSwapHtmx();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
