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
    // Add Dashboard folder to view search paths for OOB swaps
    options.PartialViewSearchPaths.Add("Dashboard");
    
    // Enable all DevTools features for the demo
    options.Diagnostics.EnableClientLogging = true;
    options.Diagnostics.EnableDevToolsPanel = true;
    options.Diagnostics.WarnOnUnhandledEvents = true;
    options.Diagnostics.WarnOnMissingOobTargets = true;
    options.Diagnostics.EnableTimingLogs = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// UseStaticFiles must come before UseRouting
// This serves wwwroot files AND RCL static web assets (_content/*)
app.UseStaticFiles();

app.UseRouting();
app.UseSwapHtmx();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
