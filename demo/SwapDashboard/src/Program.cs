using Swap.Htmx;
using SwapDashboard.Events;
using SwapDashboard.Handlers;
using SwapDashboard.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Register services
builder.Services.AddSingleton<IProjectService, ProjectService>();
builder.Services.AddSingleton<ITaskService, TaskService>();
builder.Services.AddSingleton<IActivityService, ActivityService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddSingleton<ITeamService, TeamService>();

builder.Services.AddSwapHtmx(options =>
{
    // Enable all DevTools features for the demo
    options.Diagnostics.EnableClientLogging = true;
    options.Diagnostics.EnableDevToolsPanel = true;
    options.Diagnostics.WarnOnUnhandledEvents = true;
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
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
