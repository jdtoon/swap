using Swap.Htmx;
using Swap.Htmx.Events;
using Swap.Htmx.WebSockets;
using Swap.Htmx.TestApp.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Swap.Htmx runtime wiring
builder.Services.AddSwapHtmx();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(sp =>
{
    var options = new SwapEventBusOptions();
    // Example chain: todo.created => ui.refreshList
    options.Chain(SwapEvents.Entity.Created("todo"), SwapEvents.UI.RefreshList);
    return options;
});
builder.Services.AddScoped<ISwapEventBus, SwapEventBus>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Test/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

// Register WebSocket handler
app.MapSwapWebSocket<ChatWebSocketHandler>("/ws/chat");

app.UseSwapHtmxShell();
app.UseSwapHtmx();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Test}/{action=Index}/{id?}");

// Dev endpoints for inspecting Swap event chains (Development only)
app.MapSwapHtmxDevEndpoints();

app.Run();

public partial class Program {}
