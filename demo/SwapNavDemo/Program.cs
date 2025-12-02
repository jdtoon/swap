using Swap.Htmx;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSwapHtmx();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSwapHtmx();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
