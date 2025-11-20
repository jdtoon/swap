using Swap.Htmx;
using Swap.Htmx.Models;
using TaskFlow.Services;
using TaskFlow.Events;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();

// Configure Swap.Htmx with view search paths and event chains
builder.Services.AddSwapHtmx(options =>
{
    // Configure view search paths for better organization
    options.PartialViewSearchPaths = new List<string>
    {
        "Tasks",        // /Views/Tasks/
        "Projects",     // /Views/Projects/
        "Comments",     // /Views/Comments/
        "Dashboard",    // /Views/Dashboard/
        "Notifications",// /Views/Notifications/
        "Shared"        // /Views/Shared/
    };

    // Configure event chains with payload access
    options.AddConfig<SseEventConfig>();
    options.AddConfig<TaskEventConfig>();
    options.AddConfig<ProjectEventConfig>();
    options.AddConfig<CommentEventConfig>();
    options.AddConfig<NotificationEventConfig>();
    options.AddConfig<ActivityEventConfig>();
})
.AddSseEventBridge(); // Register SSE services for event-driven broadcasting

// Add task flow services (in-memory for demo)
builder.Services.AddSingleton<ITaskService, TaskService>();
builder.Services.AddSingleton<IProjectService, ProjectService>();
builder.Services.AddSingleton<ITeamService, TeamService>();
builder.Services.AddSingleton<ICommentService, CommentService>();
builder.Services.AddSingleton<IActivityService, ActivityService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();

// Add background service for deadline monitoring (demonstrates warning toasts)
builder.Services.AddHostedService<TaskDeadlineMonitor>();

// Enable response compression for SSE
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Configure session for Swap.Htmx session helper
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseResponseCompression();

// Swap.Htmx middleware (must be after UseRouting, before MapControllers)
app.UseSwapHtmx()
   .UseSseEventBridge(); // Enable SSE event broadcasting from event chains

app.MapControllers();

// Minimal API Endpoint for Quick Notes
app.MapPost("/api/quicknote", (HttpContext context) =>
{
    var note = context.Request.Form["note"].ToString();
    
    // In a real app, save to DB
    return SwapResults.Response()
        .WithSuccessToast("Note saved!");
});

app.Run();
