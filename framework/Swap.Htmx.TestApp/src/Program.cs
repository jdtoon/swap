using Swap.Htmx;
using Swap.Htmx.WebSockets;
using Swap.Htmx.TestApp.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSwapHtmx();

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

app.Run();

public partial class Program {}
