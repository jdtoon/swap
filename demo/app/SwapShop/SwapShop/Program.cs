using Swap.Htmx;
using Swap.Htmx.Events;
using SwapShop.Events;
using SwapShop.Infrastructure;
using SwapShop.Services;
using SwapShop.Views;
using Swap.Htmx.Extensions; // For ToastType

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

// Configure session for shopping cart
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add response compression (demonstrate HTML compression)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Add Swap.Htmx with event chains
builder.Services.AddSwapHtmx(config =>
{
    // Product event chains
    config.When(ProductEvents.Viewed)
        .RefreshPartial(ProductElements.Card, ProductViews.Details, ctx => 
        {
            // Model passed via payload in SwapEvent(event, payload)
            return null; // Uses payload automatically
        });

    config.When(ProductEvents.LowStock)
        .RefreshPartial(ProductElements.Card, ProductViews.StockBadge, ctx => null)
        .Toast("Low stock alert!", ToastType.Warning);

    config.When(ProductEvents.StockChecked)
        .RefreshPartial(ProductElements.Card, ProductViews.StockBadge, ctx => null);

    // Cart event chains - the heart of the demo
    // Note: For cart events, we use model factories that retrieve current cart state
    config.When(CartEvents.ItemAdded)
        .RefreshPartial(CartElements.Badge, CartViews.Badge, ctx =>
        {
            var cartService = ctx.RequestServices.GetRequiredService<ICartService>();
            return cartService.GetItemCount(ctx.Session.Id);
        })
        .RefreshPartial(CartElements.MiniCart, CartViews.MiniCart, ctx =>
        {
            var cartService = ctx.RequestServices.GetRequiredService<ICartService>();
            return cartService.GetCart(ctx.Session.Id);
        })
        .Toast("Item added to cart", ToastType.Success);

    config.When(CartEvents.QuantityUpdated)
        .RefreshPartial(CartElements.Items, CartViews.Items, ctx =>
        {
            var cartService = ctx.RequestServices.GetRequiredService<ICartService>();
            return cartService.GetCart(ctx.Session.Id);
        })
        .RefreshPartial(CartElements.Total, CartViews.Total, ctx =>
        {
            var cartService = ctx.RequestServices.GetRequiredService<ICartService>();
            return cartService.GetCart(ctx.Session.Id);
        })
        .RefreshPartial(CartElements.Badge, CartViews.Badge, ctx =>
        {
            var cartService = ctx.RequestServices.GetRequiredService<ICartService>();
            return cartService.GetItemCount(ctx.Session.Id);
        });

    config.When(CartEvents.ItemRemoved)
        .RefreshPartial(CartElements.Items, CartViews.Items, ctx =>
        {
            var cartService = ctx.RequestServices.GetRequiredService<ICartService>();
            return cartService.GetCart(ctx.Session.Id);
        })
        .RefreshPartial(CartElements.Total, CartViews.Total, ctx =>
        {
            var cartService = ctx.RequestServices.GetRequiredService<ICartService>();
            return cartService.GetCart(ctx.Session.Id);
        })
        .RefreshPartial(CartElements.Badge, CartViews.Badge, ctx =>
        {
            var cartService = ctx.RequestServices.GetRequiredService<ICartService>();
            return cartService.GetItemCount(ctx.Session.Id);
        })
        .Toast("Item removed", ToastType.Info);

    config.When(CartEvents.Cleared)
        .RefreshPartial(CartElements.Items, CartViews.Empty, ctx => null)
        .RefreshPartial(CartElements.Badge, CartViews.Badge, ctx => 0)
        .Toast("Cart cleared", ToastType.Info);

    config.When(CartEvents.AddFailed)
        .Toast("Could not add item - insufficient stock", ToastType.Error);

    config.When(CartEvents.UpdateFailed)
        .Toast("Could not update quantity - exceeds available stock", ToastType.Error);

    // Order event chains
    config.When(OrderEvents.Created)
        .RefreshPartial(CartElements.Badge, CartViews.Badge, ctx => 0)
        .Toast("Order placed successfully!", ToastType.Success)
        .AlsoTrigger(NotificationEvents.OrderConfirmation);

    config.When(OrderEvents.Processing)
        .RefreshPartial(OrderElements.Status, OrderViews.Status, ctx => null)
        .Toast("Order is being processed", ToastType.Info);

    config.When(OrderEvents.Shipped)
        .RefreshPartial(OrderElements.Status, OrderViews.Status, ctx => null)
        .Toast("Order has been shipped!", ToastType.Success);

    config.When(OrderEvents.Delivered)
        .RefreshPartial(OrderElements.Status, OrderViews.Status, ctx => null)
        .Toast("Order delivered!", ToastType.Success);

    config.When(OrderEvents.Failed)
        .Toast("Order failed - please try again", ToastType.Error);
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

app.Run();
