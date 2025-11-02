using Swap.Htmx;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSwapHtmx(events =>
{
    // When a product is created, refresh lists that subscribe
    events.Chain(Swap.Htmx.Events.SwapEvents.Entity.Created("product"), Swap.Htmx.Events.SwapEvents.UI.RefreshList);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

// Swap.Htmx middleware for event handling
app.UseSwapHtmx();
app.UseSwapHtmxShell();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
