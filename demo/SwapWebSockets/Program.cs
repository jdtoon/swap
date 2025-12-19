using Swap.Htmx;
using Swap.Htmx.Realtime;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSwapHtmx(options => 
{
    options.AddConfig<SwapWebSockets.Events.ChatEventConfiguration>();
})
    .AddSseEventBridge(); // Enables Realtime features (SSE + WebSockets)

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.UseWebSockets(); // Enable WebSockets middleware
app.UseSwapHtmx()
   .UseSseEventBridge(); // Enable Realtime middleware

app.MapStaticAssets();

// Map WebSocket endpoint
app.MapGet("/swap/ws", (IRealtimeConnectionRegistry registry) => 
{
    return SwapRealtimeResults.WebSocket(registry, options => {
        options.AutoSubscribeRooms = new[] { "global" };
        options.OnConnected = async (conn) => {
            Console.WriteLine($"Client connected: {conn.Id}");
        };
    });
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
