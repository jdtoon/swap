using Swap.Htmx;
using SwapLab.Events;
using SwapLab.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // Insert decimal binder at the beginning to handle locale-agnostic decimals
    options.ModelBinderProviders.Insert(0, new InvariantDecimalModelBinderProvider());
});

builder.Services.AddSwapHtmx(options =>
{
    // Enable all DevTools features for the demo
    options.Diagnostics.EnableClientLogging = true;
    options.Diagnostics.EnableDevToolsPanel = true;
    options.Diagnostics.WarnOnUnhandledEvents = true;
    
    // Register event chain configurations
    options.AddConfig<ProductEventConfig>();
    options.AddConfig<TaskEventConfig>();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSwapHtmx();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
