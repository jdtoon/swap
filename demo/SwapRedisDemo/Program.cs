using Swap.Htmx;
using Swap.Htmx.Realtime;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSwapHtmx()
    .AddSseEventBridge()
    .AddSwapRedisBackplane(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
        options.ChannelName = "swap-redis-demo";
    });

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

app.UseSwapHtmx();
app.UseSseEventBridge();

app.UseAuthorization();

// app.MapStaticAssets();

app.MapGet("/swap/sse", (ISseConnectionRegistry registry) => SwapResults.Sse(registry));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
    // .WithStaticAssets();

app.Run();
