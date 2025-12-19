using Swap.Htmx;
using SwapShop.Events;
using SwapShop.Infrastructure;
using SwapShop.Services;
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews(options =>
{
    // Add invariant decimal model binder for cross-culture decimal handling
    options.ModelBinderProviders.Insert(0, new InvariantDecimalModelBinderProvider());
});

// Register application services
builder.Services.AddSingleton<IProductService, ProductService>();
builder.Services.AddSingleton<ICartService, CartService>();
builder.Services.AddSingleton<IOrderService, OrderService>();
builder.Services.AddSingleton<IReviewService, ReviewService>();

// Configure session for shopping cart
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add response compression with Brotli and Gzip
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

// Add Swap.Htmx with decentralized event configuration
builder.Services.AddSwapHtmx(options =>
{
    // Auto-suppress layout for HTMX requests - no _ViewStart.cshtml needed!
    options.AutoSuppressLayout = true;
    
    // Default navigation target for <swap-nav> tag helper
    options.DefaultNavigationTarget = "#main-content";
    
    // Configure view search paths for cross-controller OOB swaps
    options.PartialViewSearchPaths.Add("Cart");
    options.PartialViewSearchPaths.Add("Products");
    options.PartialViewSearchPaths.Add("Orders");
    
    // Register feature-specific event configurations
    options.AddConfig<CartEventConfig>();
    options.AddConfig<ProductEventConfig>();
    options.AddConfig<OrderEventConfig>();
    options.AddConfig<ReviewEventConfig>();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseResponseCompression();
app.UseSession();

// Swap.Htmx middleware
app.UseSwapHtmx();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}");

// Minimal API Endpoint for Newsletter
app.MapPost("/api/newsletter", (HttpContext context) =>
{
    var email = context.Request.Form["email"].ToString();
    
    // Simulate processing
    if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
    {
        return SwapResults.Response()
            .WithErrorToast("Please enter a valid email address.");
    }

    return SwapResults.Response()
        .WithView("_NewsletterSuccess", email)
        .WithSuccessToast("Subscribed successfully!");
});

// Minimal API GET Endpoint
app.MapGet("/api/status", () => 
{
    return SwapResults.Response()
        .WithView("_Status", "System Operational");
});

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
